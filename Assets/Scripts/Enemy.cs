using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    // ── Tunable parameters ──────────────────────────────────────────────────
    [SerializeField] private float _wanderSpeed      = 2f;
    [SerializeField] private float _chaseSpeed       = 5f;
    [SerializeField] private float _wanderRadius     = 6f;
    [SerializeField] private float _detectionRange   = 8f;   // starts chasing
    [SerializeField] private float _loseRange        = 13f;  // gives up chasing
    [SerializeField] private float _stealRange       = 1.5f; // must be this close to steal
    [SerializeField] private float _scareRange       = 3.5f; // player charging inside this = scared
    [SerializeField] private float _scaredDuration   = 8f;
    [SerializeField] private int   _stealAmount      = 5;
    [SerializeField] private float _stealCooldown    = 4f;

    // ── State ────────────────────────────────────────────────────────────────
    private enum State { Wander, Chase, Scared, Returning }
    private State _state = State.Wander;

    [HideInInspector] public Transform spawnPoint;

    private CharacterController _cc;
    private Transform _player;
    private PlayerController _playerCtrl;
    private Vector3 _wanderTarget;
    private float _nextWanderTime;
    private float _lastStealTime = -99f;
    private bool _isScared;

    private const float Gravity       = -9.81f;
    private const float StealPopScale = 1.4f;
    private const float StealPopTime  = 0.12f;

    void Start()
    {
        _cc = GetComponent<CharacterController>();

        var playerGO = GameObject.FindWithTag(Tags.Player);
        if (playerGO != null)
        {
            _player     = playerGO.transform;
            _playerCtrl = playerGO.GetComponent<PlayerController>();
        }

        PickWanderTarget();
    }

    void Update()
    {
        if (_player == null) return;

        switch (_state)
        {
            case State.Wander:    UpdateWander();    break;
            case State.Chase:     UpdateChase();     break;
            case State.Scared:                       break; // coroutine drives scared movement
            case State.Returning: UpdateReturning(); break;
        }
    }

    // ── Wander ───────────────────────────────────────────────────────────────

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
        Vector2 circle = Random.insideUnitCircle * _wanderRadius;
        Vector3 origin = spawnPoint != null ? spawnPoint.position : transform.position;
        _wanderTarget  = origin + new Vector3(circle.x, 0f, circle.y);
        _nextWanderTime = Time.time + Random.Range(3f, 6f);
    }

    // ── Chase ────────────────────────────────────────────────────────────────

    void UpdateChase()
    {
        float dist = Vector3.Distance(transform.position, _player.position);

        // Player no longer carrying anything, or ran too far away
        if (!_playerCtrl.hasMat || dist > _loseRange)
        {
            _state = State.Wander;
            return;
        }

        // Check if player is charging at us
        if (dist < _scareRange && IsPlayerChargingAtMe())
        {
            StartCoroutine(ScaredRoutine());
            return;
        }

        // Close enough to steal
        if (dist <= _stealRange && Time.time - _lastStealTime >= _stealCooldown)
        {
            TrySteal();
            return;
        }

        MoveToward(_player.position, _chaseSpeed);
    }

    bool IsPlayerChargingAtMe()
    {
        // Player's CharacterController velocity toward this enemy above a threshold
        var playerCC = _player.GetComponent<CharacterController>();
        if (playerCC == null) return false;

        Vector3 vel = playerCC.velocity;
        if (vel.magnitude < 2.5f) return false;

        Vector3 toEnemy = (transform.position - _player.position).normalized;
        return Vector3.Dot(vel.normalized, toEnemy) > 0.7f;
    }

    void TrySteal()
    {
        _lastStealTime = Time.time;
        int woodCount = _playerCtrl.currentElementsWood.Count;
        int fishCount = _playerCtrl.currentElementsFish.Count;
        int total = woodCount + fishCount;
        if (total == 0) return;

        // Steal from whichever resource the player has more of
        int amount = Mathf.Min(_stealAmount, total);
        if (fishCount >= woodCount)
        {
            int take = Mathf.Min(amount, fishCount);
            for (int i = 0; i < take; i++)
            {
                GameObject item = _playerCtrl.currentElementsFish[0];
                _playerCtrl.currentElementsFish.RemoveAt(0);
                Destroy(item);
            }
        }
        else
        {
            int take = Mathf.Min(amount, woodCount);
            for (int i = 0; i < take; i++)
            {
                GameObject item = _playerCtrl.currentElementsWood[0];
                _playerCtrl.currentElementsWood.RemoveAt(0);
                Destroy(item);
            }
        }

        // Squash-and-stretch celebration
        LeanTween.cancel(gameObject);
        LeanTween.scale(gameObject, new Vector3(StealPopScale, 1f / StealPopScale, StealPopScale), StealPopTime)
            .setEaseOutQuad()
            .setOnComplete(() => LeanTween.scale(gameObject, Vector3.one, StealPopTime * 2f).setEaseOutBack());

        // Run back to spawn
        _state = State.Returning;
    }

    // ── Scared ───────────────────────────────────────────────────────────────

    IEnumerator ScaredRoutine()
    {
        _state    = State.Scared;
        _isScared = true;

        float endTime = Time.time + _scaredDuration;
        while (Time.time < endTime)
        {
            // Flee directly away from player
            Vector3 fleeDir = (transform.position - _player.position).normalized;
            MoveToward(transform.position + fleeDir * 2f, _chaseSpeed * 1.5f);
            yield return null;
        }

        _isScared = false;
        _state    = State.Returning;
    }

    // ── Returning ─────────────────────────────────────────────────────────────

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
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 0.15f);

        Vector3 move = dir.normalized * speed * Time.deltaTime;
        move.y = Gravity * Time.deltaTime; // simple gravity
        _cc.Move(move);
    }

    bool ReachedTarget(Vector3 target)
    {
        Vector3 flat = target - transform.position;
        flat.y = 0f;
        return flat.sqrMagnitude < 0.4f;
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
