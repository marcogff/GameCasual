using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Enemy : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────────────────────────
    [Header("Movement")]
    [SerializeField] private float _wanderSpeed    = 2f;
    [SerializeField] private float _chaseSpeed     = 5f;
    [SerializeField] private float _scaredSpeed    = 7f;
    [SerializeField] private float _wanderRadius   = 8f;

    [Header("Detection")]
    [SerializeField] private float _detectionRange = 10f;
    [SerializeField] private float _loseRange      = 15f;

    [Header("Stealing")]
    [SerializeField] private float _stealRadius    = 1.2f;
    [SerializeField] private int   _stealAmount    = 5;
    [SerializeField] private float _stealCooldown  = 3f;

    [Header("Scared")]
    [SerializeField] private float _scareRange     = 3.5f;
    [SerializeField] private float _scaredDuration = 6f;

    // ── State ────────────────────────────────────────────────────────────────
    private enum State { Wander, Chase, Scared, Returning }
    private State _state = State.Wander;

    [HideInInspector] public Transform spawnPoint;

    // ── Components ───────────────────────────────────────────────────────────
    private NavMeshAgent     _agent;
    private Animator         _animator;
    private Transform        _player;
    private PlayerController _playerCtrl;
    private WolfAlert        _alert;

    // ── Networking (Phase 4) ──────────────────────────────────────────────────
    // Solo: _netObj is null → all of this stays inert and the wolf behaves normally.
    // Multiplayer: AI runs only on the server; remote clients are driven by
    // NetworkTransform and just animate from observed movement.
    private NetworkObject _netObj;
    private bool          _replicaInit;
    private Vector3       _lastPos;

    // ── Animation ────────────────────────────────────────────────────────────
    private bool         _wasMoving;
    private bool         _warnedPartialPath;
    private Vector3      _baseScale = Vector3.one;  // the wolf's real scale (prefab is 3x)
    private const string AnimIdle = "Idle";
    private const string AnimRun  = "Run";

    // ── Misc ─────────────────────────────────────────────────────────────────
    private float _lastStealTime     = -99f;
    private float _lastDestUpdate    = 0f;
    private float _lastTargetRefresh = 0f;
    private float _chaseStartTime    = 0f;

    private const float DestUpdateInterval    = 0.1f;  // cap destination writes to 10/s
    private const float TargetRefreshInterval = 5f;    // re-evaluate nearest player every 5s
    private const float MaxChaseDuration      = 12f;   // give up the chase after this long
    private const float StealPopScale      = 1.4f;
    private const float StealPopTime       = 0.12f;

    // ── Start ────────────────────────────────────────────────────────────────

    void Start()
    {
        _agent    = GetComponent<NavMeshAgent>();
        _animator = GetComponentInChildren<Animator>();
        _alert    = gameObject.AddComponent<WolfAlert>();
        _baseScale = transform.localScale; // capture real scale so the steal pop restores it
        _netObj   = GetComponent<NetworkObject>(); // null in solo — networking stays inert
        _lastPos  = transform.position;

        _agent.speed            = _wanderSpeed;
        _agent.acceleration     = 12f;
        _agent.angularSpeed     = 360f;
        _agent.stoppingDistance = 0.3f;
        _agent.autoBraking      = false; // chaser shouldn't slow down near waypoints
        _agent.isStopped        = false;

        // Hit trigger — stealing on contact; added here so no prefab edits are needed.
        var hit   = gameObject.AddComponent<SphereCollider>();
        hit.isTrigger = true;
        hit.radius    = _stealRadius;

        // Trigger events (OnTriggerEnter/Stay) only fire if at least one of the two
        // colliders has a Rigidbody. The NavMeshAgent moves by transform, not physics,
        // so add a kinematic Rigidbody here to guarantee steal-on-contact works.
        var rb = gameObject.GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity  = false;

        // Find the nearest player — handles both solo and 2-player sessions
        if (!RefreshTarget())
            Debug.LogWarning("[Enemy] No player with tag 'Player' found in scene.");

        if (!_agent.isOnNavMesh)
        {
            Debug.LogError("[Enemy] Wolf is NOT on the NavMesh!\n" +
                           "1. Select terrain → Inspector → Static → Navigation Static\n" +
                           "2. Window → AI → Navigation → Bake\n" +
                           "3. Blue overlay = walkable. Wolf works after that.");
            return;
        }

        PickWanderTarget();
    }

    // ── Update ───────────────────────────────────────────────────────────────

    void Update()
    {
        // On a remote client this wolf is a replica — NetworkTransform moves it;
        // don't run AI here (it would fight the server). Just animate.
        if (IsRemoteReplica()) { DriveReplica(); return; }

        if (!_agent.isOnNavMesh) return;

        // Periodically re-target the nearest player so the wolf switches
        // to player 2 if they're closer (also handles late-join in multiplayer).
        if (Time.time - _lastTargetRefresh > TargetRefreshInterval)
        {
            _lastTargetRefresh = Time.time;
            RefreshTarget();
        }

        if (_player == null || _playerCtrl == null) return;

        switch (_state)
        {
            case State.Wander:    UpdateWander();    break;
            case State.Chase:     UpdateChase();     break;
            case State.Scared:    /* coroutine */    break;
            case State.Returning: UpdateReturning(); break;
        }

        SyncAnimation();
    }

    // ── Player targeting ─────────────────────────────────────────────────────

    /// <summary>
    /// Finds the nearest PlayerController in the scene and updates _player / _playerCtrl.
    /// Returns true if a target was found.
    /// In solo play this always returns the one player.
    /// In multiplayer (Phase 4+) the wolf will chase whoever is closest.
    /// </summary>
    bool RefreshTarget()
    {
        var all = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        if (all.Length == 0) return false;

        float   nearest = float.MaxValue;
        PlayerController best = null;

        foreach (var pc in all)
        {
            float d = Vector3.Distance(transform.position, pc.transform.position);
            if (d < nearest) { nearest = d; best = pc; }
        }

        if (best == null) return false;
        _player     = best.transform;
        _playerCtrl = best;
        return true;
    }

    // ── Wander ───────────────────────────────────────────────────────────────

    void UpdateWander()
    {
        float dist = Vector3.Distance(transform.position, _player.position);

        if (dist <= _detectionRange)
        {
            EnterChase();
            return;
        }

        if (!_agent.pathPending && _agent.remainingDistance < 0.5f)
            PickWanderTarget();
    }

    void EnterChase()
    {
        _state           = State.Chase;
        _agent.speed     = _chaseSpeed;
        _agent.isStopped = false;     // guard against a stuck-stopped state from a prior steal
        _chaseStartTime  = Time.time;
        _alert?.Show();               // telegraph the attack to the player
        AudioManager.Instance.Play(AudioManager.WolfAlert);

        // Set the destination immediately — don't wait a frame or for the throttle,
        // otherwise the wolf appears to "see" the player ("!") but stand still.
        ChaseStep();
        _warnedPartialPath = false;
    }

    void PickWanderTarget()
    {
        Vector3 origin = spawnPoint != null ? spawnPoint.position : transform.position;

        for (int i = 0; i < 8; i++)
        {
            Vector2 circle    = Random.insideUnitCircle * _wanderRadius;
            Vector3 candidate = origin + new Vector3(circle.x, 0f, circle.y);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            {
                _agent.SetDestination(hit.position);
                return;
            }
        }

        _agent.SetDestination(origin);
    }

    // ── Chase ────────────────────────────────────────────────────────────────

    void UpdateChase()
    {
        float dist = Vector3.Distance(transform.position, _player.position);

        // Give up if the player escapes range, or after chasing too long without a steal
        if (dist > _loseRange || Time.time - _chaseStartTime > MaxChaseDuration)
        {
            _state       = State.Wander;
            _agent.speed = _wanderSpeed;
            _alert?.Hide();
            PickWanderTarget();
            return;
        }

        if (dist < _scareRange && IsPlayerChargingAtMe())
        {
            _alert?.Hide();
            StartCoroutine(ScaredRoutine());
            return;
        }

        // Keep the alert pinned while actively chasing
        _agent.isStopped = false;

        // Throttle destination writes — NavMesh handles internal path smoothing;
        // setting destination every frame is wasteful.
        if (Time.time - _lastDestUpdate > DestUpdateInterval)
            ChaseStep();
    }

    /// <summary>Points the agent at the current player position (sampled onto the NavMesh).</summary>
    void ChaseStep()
    {
        _lastDestUpdate = Time.time;
        if (NavMesh.SamplePosition(_player.position, out NavMeshHit navHit, 5f, NavMesh.AllAreas))
            _agent.SetDestination(navHit.position);
        else
            _agent.SetDestination(_player.position);

#if UNITY_EDITOR
        // If the agent can't fully reach the player, the NavMesh isn't connected
        // between them (gap, unbaked area, or freshly-unlocked land). Warn once.
        if (!_warnedPartialPath && !_agent.pathPending &&
            _agent.pathStatus != NavMeshPathStatus.PathComplete)
        {
            _warnedPartialPath = true;
            Debug.LogWarning(
                "[Enemy] Wolf can't reach the player — NavMesh path is " +
                $"{_agent.pathStatus}. The walkable area is not fully baked/connected.\n" +
                "Fix: Window → AI → Navigation → Bake, covering the WHOLE walkable map " +
                "(including any land the player unlocks). Until then the wolf can only " +
                "chase within the baked area.");
        }
#endif
    }

    // ── Hit detection (steal on contact) ─────────────────────────────────────

    void OnTriggerEnter(Collider other)
    {
        if (IsRemoteReplica()) return;   // steal is server-authoritative
        if (_state == State.Scared) return;
        if (!other.CompareTag(Tags.Player)) return;
        AttemptSteal();
    }

    void OnTriggerStay(Collider other)
    {
        if (IsRemoteReplica()) return;
        if (_state == State.Scared) return;
        if (!other.CompareTag(Tags.Player)) return;
        AttemptSteal();
    }

    void AttemptSteal()
    {
        if (Time.time - _lastStealTime < _stealCooldown) return;
        if (_playerCtrl == null) return;

        int carried = _playerCtrl.CarriedCount;
        if (carried == 0) return;

        _lastStealTime   = Time.time;
        _agent.isStopped = true;

        // Delegate the actual removal to the player. In multiplayer this runs on the
        // server and routes to the targeted client (server-authoritative); the
        // victim's screen flash + steal sound fire there. In solo it's immediate.
        int amount = Mathf.Min(_stealAmount, carried);
        _playerCtrl.StealFromWolf(amount);

        // Squash-and-stretch pop RELATIVE to the wolf's real scale, then run home.
        // (Bug was scaling back to Vector3.one — the prefab is 3x, so it shrank.)
        LeanTween.cancel(gameObject);
        Vector3 squashed = Vector3.Scale(_baseScale,
            new Vector3(StealPopScale, 1f / StealPopScale, StealPopScale));
        LeanTween.scale(gameObject, squashed, StealPopTime)
            .setEaseOutQuad()
            .setOnComplete(() =>
            {
                LeanTween.scale(gameObject, _baseScale, StealPopTime * 2f).setEaseOutBack();
                StartReturning();
            });
    }

    // ── Scared ───────────────────────────────────────────────────────────────

    bool IsPlayerChargingAtMe()
    {
        Vector3 vel = _playerCtrl.targetTransform.velocity;
        if (vel.magnitude < 2.5f) return false;
        Vector3 toEnemy = (transform.position - _player.position).normalized;
        return Vector3.Dot(vel.normalized, toEnemy) > 0.7f;
    }

    IEnumerator ScaredRoutine()
    {
        _state       = State.Scared;
        _agent.speed = _scaredSpeed;

        float end = Time.time + _scaredDuration;
        while (Time.time < end)
        {
            Vector3 fleeDir    = (transform.position - _player.position).normalized;
            Vector3 fleeTarget = transform.position + fleeDir * 6f;

            if (NavMesh.SamplePosition(fleeTarget, out NavMeshHit hit, 4f, NavMesh.AllAreas))
                _agent.SetDestination(hit.position);

            yield return new WaitForSeconds(0.5f);
        }

        StartReturning();
    }

    // ── Returning ────────────────────────────────────────────────────────────

    void StartReturning()
    {
        _state           = State.Returning;
        _agent.isStopped = false;
        _agent.speed     = _wanderSpeed;

        if (spawnPoint != null)
            _agent.SetDestination(spawnPoint.position);
        else
            PickWanderTarget();
    }

    void UpdateReturning()
    {
        if (spawnPoint == null) { _state = State.Wander; return; }

        if (!_agent.pathPending && _agent.remainingDistance < 0.5f)
        {
            _state       = State.Wander;
            _agent.speed = _wanderSpeed;
            PickWanderTarget();
        }
    }

    // ── Animation ────────────────────────────────────────────────────────────

    void SyncAnimation() => SetMoving(_agent.velocity.magnitude > 0.15f);

    void SetMoving(bool isMoving)
    {
        if (_animator == null) return;
        if (isMoving == _wasMoving) return;
        _animator.CrossFade(isMoving ? AnimRun : AnimIdle, 0.15f);
        _wasMoving = isMoving;
    }

    // ── Networking (Phase 4) ──────────────────────────────────────────────────

    /// <summary>True only for a wolf replica on a non-server client.</summary>
    bool IsRemoteReplica()
    {
        if (_netObj == null) return false;
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return false;
        return _netObj.IsSpawned && !NetworkManager.Singleton.IsServer;
    }

    /// <summary>On a client, the NavMeshAgent must not drive movement (NetworkTransform does).
    /// Animate from observed position change instead.</summary>
    void DriveReplica()
    {
        if (!_replicaInit)
        {
            _replicaInit = true;
            if (_agent != null) { _agent.updatePosition = false; _agent.updateRotation = false; }
            _lastPos = transform.position;
        }

        float speed = (transform.position - _lastPos).magnitude / Mathf.Max(Time.deltaTime, 1e-4f);
        _lastPos = transform.position;
        SetMoving(speed > 0.15f);
    }

    // ── Gizmos ───────────────────────────────────────────────────────────────
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectionRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _loseRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _scareRange);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, _stealRadius);
    }
#endif
}
