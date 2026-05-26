using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    // ── Tunable parameters ──────────────────────────────────────────────────
    [SerializeField] private float _wanderSpeed    = 2f;
    [SerializeField] private float _chaseSpeed     = 5f;
    [SerializeField] private float _wanderRadius   = 6f;
    [SerializeField] private float _detectionRange = 8f;
    [SerializeField] private float _loseRange      = 13f;
    [SerializeField] private float _stealRange     = 1.5f;
    [SerializeField] private float _scareRange     = 3.5f;
    [SerializeField] private float _scaredDuration = 8f;
    [SerializeField] private int   _stealAmount    = 5;
    [SerializeField] private float _stealCooldown  = 4f;

    // ── State machine ────────────────────────────────────────────────────────
    private enum State { Wander, Chase, Scared, Returning }
    private State _state = State.Wander;

    [HideInInspector] public Transform spawnPoint;

    // ── Components ───────────────────────────────────────────────────────────
    private CharacterController _cc;
    private Animator            _animator;
    private Transform           _player;
    private PlayerController    _playerCtrl;

    // ── Internal ─────────────────────────────────────────────────────────────
    private Vector3 _wanderTarget;
    private float   _nextWanderTime;
    private float   _lastStealTime  = -99f;
    private float   _verticalVelocity;
    private bool    _isMoving;          // true while MoveToward() is called this frame
    private bool    _wasMoving;         // previous frame — avoids calling CrossFade every frame

    private const float Gravity       = -15f;
    private const float StealPopScale = 1.4f;
    private const float StealPopTime  = 0.12f;

    // Animation state names — must match what WolfAnimatorSetup creates
    private const string AnimIdle = "Idle";
    private const string AnimRun  = "Run";

    // ── Start ────────────────────────────────────────────────────────────────

    void Start()
    {
        _cc       = GetComponent<CharacterController>();
        _animator = GetComponentInChildren<Animator>();

        if (_animator == null)
            Debug.LogWarning($"[Enemy] '{name}': No Animator found.");

        var playerGO = GameObject.FindWithTag(Tags.Player);
        if (playerGO != null)
        {
            _player     = playerGO.transform;
            _playerCtrl = playerGO.GetComponent<PlayerController>();
        }
        else
        {
            Debug.LogWarning("[Enemy] Player not found — set the player's Tag to 'Player'.");
        }

        SnapToGround();
        PickWanderTarget();
    }

    // ── Update ───────────────────────────────────────────────────────────────

    void Update()
    {
        if (_player == null || _playerCtrl == null) return;

        _isMoving = false;  // MoveToward() sets this to true when called

        switch (_state)
        {
            case State.Wander:    UpdateWander();    break;
            case State.Chase:     UpdateChase();     break;
            case State.Scared:    /* coroutine */    break;
            case State.Returning: UpdateReturning(); break;
        }

        // ── Animation ───────────────────────────────────────────────────────
        // CrossFade by state NAME — no parameters, no hashes, no type guessing.
        // Only call it when the moving state actually changes to avoid spamming.
        if (_animator != null && _isMoving != _wasMoving)
        {
            _animator.CrossFade(_isMoving ? AnimRun : AnimIdle, 0.15f);
            _wasMoving = _isMoving;
        }
    }

    // ── States ───────────────────────────────────────────────────────────────

    void UpdateWander()
    {
        float dist = Vector3.Distance(transform.position, _player.position);
        if (dist <= _detectionRange && _playerCtrl.hasMat)
        {
            _state = State.Chase;
            return;
        }

        MoveToward(_wanderTarget, _wanderSpeed);

        if (Time.time >= _nextWanderTime || ReachedTarget(_wanderTarget))
            PickWanderTarget();
    }

    void PickWanderTarget()
    {
        Vector2 circle  = Random.insideUnitCircle * _wanderRadius;
        Vector3 origin  = spawnPoint != null ? spawnPoint.position : transform.position;
        _wanderTarget   = origin + new Vector3(circle.x, 0f, circle.y);
        _nextWanderTime = Time.time + Random.Range(3f, 6f);
    }

    void UpdateChase()
    {
        float dist = Vector3.Distance(transform.position, _player.position);

        if (!_playerCtrl.hasMat || dist > _loseRange)
        {
            _state = State.Wander;
            return;
        }

        if (dist < _scareRange && IsPlayerChargingAtMe())
        {
            StartCoroutine(ScaredRoutine());
            return;
        }

        if (dist <= _stealRange && Time.time - _lastStealTime >= _stealCooldown)
        {
            TrySteal();
            return;
        }

        MoveToward(_player.position, _chaseSpeed);
    }

    bool IsPlayerChargingAtMe()
    {
        Vector3 vel = _playerCtrl.targetTransform.velocity;
        if (vel.magnitude < 2.5f) return false;
        Vector3 toEnemy = (transform.position - _player.position).normalized;
        return Vector3.Dot(vel.normalized, toEnemy) > 0.7f;
    }

    void TrySteal()
    {
        _lastStealTime = Time.time;

        int wood  = _playerCtrl.currentElementsWood.Count;
        int fish  = _playerCtrl.currentElementsFish.Count;
        int total = wood + fish;

        if (total == 0) { _state = State.Returning; return; }

        int amount = Mathf.Min(_stealAmount, total);
        if (fish >= wood)
        {
            int take = Mathf.Min(amount, fish);
            for (int i = 0; i < take; i++)
            {
                Destroy(_playerCtrl.currentElementsFish[0]);
                _playerCtrl.currentElementsFish.RemoveAt(0);
            }
        }
        else
        {
            int take = Mathf.Min(amount, wood);
            for (int i = 0; i < take; i++)
            {
                Destroy(_playerCtrl.currentElementsWood[0]);
                _playerCtrl.currentElementsWood.RemoveAt(0);
            }
        }

        LeanTween.cancel(gameObject);
        LeanTween.scale(gameObject,
            new Vector3(StealPopScale, 1f / StealPopScale, StealPopScale), StealPopTime)
            .setEaseOutQuad()
            .setOnComplete(() =>
                LeanTween.scale(gameObject, Vector3.one, StealPopTime * 2f).setEaseOutBack());

        _state = State.Returning;
    }

    IEnumerator ScaredRoutine()
    {
        _state = State.Scared;
        float endTime = Time.time + _scaredDuration;
        while (Time.time < endTime)
        {
            Vector3 fleeDir = (transform.position - _player.position).normalized;
            MoveToward(transform.position + fleeDir * 2f, _chaseSpeed * 1.5f);
            yield return null;
        }
        _state = State.Returning;
    }

    void UpdateReturning()
    {
        if (spawnPoint == null) { _state = State.Wander; return; }
        MoveToward(spawnPoint.position, _wanderSpeed);
        if (ReachedTarget(spawnPoint.position))
        {
            PickWanderTarget();
            _state = State.Wander;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    void MoveToward(Vector3 target, float speed)
    {
        Vector3 dir = target - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(
                transform.rotation, Quaternion.LookRotation(dir), 0.15f);

        if (_cc.isGrounded)
            _verticalVelocity = -1f;
        else
            _verticalVelocity += Gravity * Time.deltaTime;

        Vector3 move = dir.normalized * speed;
        move.y = _verticalVelocity;
        _cc.Move(move * Time.deltaTime);

        _isMoving = true;   // tells Update's animation block to play Run
    }

    bool ReachedTarget(Vector3 target)
    {
        Vector3 flat = target - transform.position;
        flat.y = 0f;
        return flat.sqrMagnitude < 0.4f;
    }

    void SnapToGround()
    {
        _cc.enabled = false;
        if (Physics.Raycast(transform.position + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 30f))
        {
            Vector3 p = transform.position;
            p.y = hit.point.y;
            transform.position = p;
        }
        _cc.enabled = true;
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _scareRange);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, _stealRange);
    }
#endif
}
