using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    // ── Tunable parameters ──────────────────────────────────────────────────
    [SerializeField] private float  _wanderSpeed     = 2f;
    [SerializeField] private float  _chaseSpeed      = 5f;
    [SerializeField] private float  _wanderRadius    = 6f;
    [SerializeField] private float  _detectionRange  = 8f;   // starts chasing
    [SerializeField] private float  _loseRange       = 13f;  // gives up chasing
    [SerializeField] private float  _stealRange      = 1.5f; // must be this close to steal
    [SerializeField] private float  _scareRange      = 3.5f; // player charging inside = scared
    [SerializeField] private float  _scaredDuration  = 8f;
    [SerializeField] private int    _stealAmount     = 5;
    [SerializeField] private float  _stealCooldown   = 4f;

    // Name of the Animator parameter that switches run ↔ idle.
    // Must match WolfAnimatorSetup.SpeedParam ("Speed").
    // On Start the Console prints every parameter found — check there if
    // animations still don't play.
    [SerializeField] private string _runParam        = "Speed";

    // ── State ────────────────────────────────────────────────────────────────
    private enum State { Wander, Chase, Scared, Returning }
    private State _state = State.Wander;

    [HideInInspector] public Transform spawnPoint;

    private CharacterController _cc;
    private Animator            _animator;
    private int                 _runParamHash;
    // True = drive with SetBool, False = drive with SetFloat (Speed-style blend trees)
    private bool                _animParamIsBool = true;

    private Transform        _player;
    private PlayerController _playerCtrl;

    private Vector3 _wanderTarget;
    private float   _nextWanderTime;
    private float   _lastStealTime = -99f;

    // Accumulated vertical velocity — lets gravity build up correctly
    // instead of being reset to Gravity*dt every frame.
    private float _verticalVelocity;

    // Set by MoveToward() each frame so the animation sync doesn't have to
    // rely on CharacterController.velocity, which can read near-zero even
    // while the enemy is moving (CC reports actual displacement, not intent).
    private float _currentSpeed;

    private const float Gravity       = -15f;
    private const float StealPopScale = 1.4f;
    private const float StealPopTime  = 0.12f;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    void Start()
    {
        _cc = GetComponent<CharacterController>();

        // Animator may live on a child (the actual mesh/rig), search children too
        _animator = GetComponentInChildren<Animator>();
        SetupAnimator();

        // IMPORTANT: The player GameObject must have the "Player" tag set in Unity.
        var playerGO = GameObject.FindWithTag(Tags.Player);
        if (playerGO != null)
        {
            _player     = playerGO.transform;
            _playerCtrl = playerGO.GetComponent<PlayerController>();
        }
        else
        {
            Debug.LogWarning("[Enemy] No GameObject with tag 'Player' found. " +
                             "Select your player in the Hierarchy → Inspector → Tag → Player.");
        }

        SnapToGround();
        PickWanderTarget();
    }

    // Inspects the Animator Controller, logs every parameter so the correct name
    // is visible in the Console, then resolves which type to drive (bool / float).
    void SetupAnimator()
    {
        if (_animator == null)
        {
            Debug.LogWarning($"[Enemy] '{name}': No Animator found on this GameObject or its children.");
            return;
        }

        // ── Print every parameter so you can copy the exact name ──────────
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"[Enemy] '{name}': Animator found on '{_animator.gameObject.name}'. Parameters:");
        foreach (var p in _animator.parameters)
            sb.AppendLine($"  name='{p.name}'  type={p.type}");
        Debug.Log(sb.ToString());

        // ── Find the configured parameter ──────────────────────────────────
        _runParamHash = Animator.StringToHash(_runParam);
        bool found = false;
        foreach (var p in _animator.parameters)
        {
            if (p.nameHash != _runParamHash) continue;
            found = true;
            _animParamIsBool = p.type == AnimatorControllerParameterType.Bool;
            Debug.Log($"[Enemy] '{name}': Using '{p.name}' as {(p.type)} for run/idle.");
            break;
        }

        if (!found)
            Debug.LogWarning($"[Enemy] '{name}': Parameter '{_runParam}' not found in the Animator Controller. " +
                             "Check the 'Run Param' field on the Enemy component and match it to one of the names logged above.");
    }

    // Raycasts straight down and repositions the enemy flush with the ground.
    // Runs once on Start so floating/clipping caused by spawner Y-offset or
    // CharacterController center mismatch is corrected before the first frame.
    void SnapToGround()
    {
        // Temporarily disable the CC so we can teleport the transform directly
        _cc.enabled = false;

        Vector3 origin = transform.position + Vector3.up * 5f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 30f))
        {
            Vector3 p = transform.position;
            p.y = hit.point.y;
            transform.position = p;
        }

        _cc.enabled = true;
    }

    void Update()
    {
        if (_player == null || _playerCtrl == null) return;

        _currentSpeed = 0f; // MoveToward() overwrites this when called

        switch (_state)
        {
            case State.Wander:    UpdateWander();    break;
            case State.Chase:     UpdateChase();     break;
            case State.Scared:    /* coroutine */    break;
            case State.Returning: UpdateReturning(); break;
        }

        // ── Animation sync ──────────────────────────────────────────────────
        // _currentSpeed is the intended movement speed set by MoveToward().
        // Using _cc.velocity here was unreliable — CC reports actual post-
        // collision displacement which can be near-zero even while moving.
        if (_animator != null)
        {
            if (_animParamIsBool)
                _animator.SetBool(_runParamHash, _currentSpeed > 0.1f);
            else
                _animator.SetFloat(_runParamHash, _currentSpeed);
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
        Vector2 circle  = Random.insideUnitCircle * _wanderRadius;
        Vector3 origin  = spawnPoint != null ? spawnPoint.position : transform.position;
        _wanderTarget   = origin + new Vector3(circle.x, 0f, circle.y);
        _nextWanderTime = Time.time + Random.Range(3f, 6f);
    }

    // ── Chase ────────────────────────────────────────────────────────────────

    void UpdateChase()
    {
        float dist = Vector3.Distance(transform.position, _player.position);

        // Player dropped everything or ran away — go back to wandering
        if (!_playerCtrl.hasMat || dist > _loseRange)
        {
            _state = State.Wander;
            return;
        }

        // Scared if player charges at us
        if (dist < _scareRange && IsPlayerChargingAtMe())
        {
            StartCoroutine(ScaredRoutine());
            return;
        }

        // Close enough — try to steal
        if (dist <= _stealRange && Time.time - _lastStealTime >= _stealCooldown)
        {
            TrySteal();
            return;
        }

        MoveToward(_player.position, _chaseSpeed);
    }

    bool IsPlayerChargingAtMe()
    {
        // The player's own CharacterController doesn't store velocity (movement is
        // applied to the ghost targetTransform). Use that velocity instead.
        Vector3 vel = _playerCtrl.targetTransform.velocity;
        if (vel.magnitude < 2.5f) return false;

        Vector3 toEnemy = (transform.position - _player.position).normalized;
        return Vector3.Dot(vel.normalized, toEnemy) > 0.7f;
    }

    void TrySteal()
    {
        _lastStealTime = Time.time;

        int woodCount = _playerCtrl.currentElementsWood.Count;
        int fishCount = _playerCtrl.currentElementsFish.Count;
        int total     = woodCount + fishCount;

        // Nothing left to steal — head home
        if (total == 0)
        {
            _state = State.Returning;
            return;
        }

        // Steal from whichever pile is bigger
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

        // Squash-and-stretch celebration then run home
        LeanTween.cancel(gameObject);
        LeanTween.scale(gameObject, new Vector3(StealPopScale, 1f / StealPopScale, StealPopScale), StealPopTime)
            .setEaseOutQuad()
            .setOnComplete(() => LeanTween.scale(gameObject, Vector3.one, StealPopTime * 2f).setEaseOutBack());

        _state = State.Returning;
    }

    // ── Scared ───────────────────────────────────────────────────────────────

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

    // ── Movement ──────────────────────────────────────────────────────────────

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

        // Record intended speed so Update's animation sync has a reliable value.
        _currentSpeed = speed;
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
