using System.Collections;
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
    [SerializeField] private float _detectionRange = 10f;  // always chase player in this range
    [SerializeField] private float _loseRange      = 15f;  // give up if player gets this far

    [Header("Stealing")]
    [SerializeField] private float _stealRadius    = 1.2f; // trigger collider size
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

    // ── Animation ────────────────────────────────────────────────────────────
    private bool         _wasMoving;
    private const string AnimIdle = "Idle";
    private const string AnimRun  = "Run";

    // ── Misc ─────────────────────────────────────────────────────────────────
    private float _lastStealTime = -99f;
    private const float StealPopScale = 1.4f;
    private const float StealPopTime  = 0.12f;

    // ── Start ────────────────────────────────────────────────────────────────

    void Start()
    {
        _agent    = GetComponent<NavMeshAgent>();
        _animator = GetComponentInChildren<Animator>();

        _agent.speed            = _wanderSpeed;
        _agent.acceleration     = 12f;
        _agent.angularSpeed     = 360f;
        _agent.stoppingDistance = 0.3f;
        _agent.autoBraking      = true;

        // ── Hit trigger collider ─────────────────────────────────────────────
        // A sphere trigger around the wolf — when the player enters it while
        // the wolf is chasing, resources are stolen immediately (hit mechanic).
        // This is added in code so no manual prefab setup is needed.
        var hit = gameObject.AddComponent<SphereCollider>();
        hit.isTrigger = true;
        hit.radius    = _stealRadius;

        // ── Find player ──────────────────────────────────────────────────────
        var playerGO = GameObject.FindWithTag(Tags.Player);
        if (playerGO != null)
        {
            _player     = playerGO.transform;
            _playerCtrl = playerGO.GetComponent<PlayerController>();
        }
        else
        {
            Debug.LogWarning("[Enemy] Tag 'Player' not set on the player GameObject.");
        }

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
        if (_player == null || _playerCtrl == null) return;
        if (!_agent.isOnNavMesh) return;

        switch (_state)
        {
            case State.Wander:    UpdateWander();    break;
            case State.Chase:     UpdateChase();     break;
            case State.Scared:    /* coroutine */    break;
            case State.Returning: UpdateReturning(); break;
        }

        SyncAnimation();
    }

    // ── Wander ───────────────────────────────────────────────────────────────

    void UpdateWander()
    {
        float dist = Vector3.Distance(transform.position, _player.position);

        // Always chase if player is close — steal only triggers when they have resources
        if (dist <= _detectionRange)
        {
            _state       = State.Chase;
            _agent.speed = _chaseSpeed;
            return;
        }

        if (!_agent.pathPending && _agent.remainingDistance < 0.5f)
            PickWanderTarget();
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

        // Lost the player
        if (dist > _loseRange)
        {
            _state       = State.Wander;
            _agent.speed = _wanderSpeed;
            PickWanderTarget();
            return;
        }

        // Player charging at us → flee
        if (dist < _scareRange && IsPlayerChargingAtMe())
        {
            StartCoroutine(ScaredRoutine());
            return;
        }

        // Keep following — stealing is handled by OnTriggerEnter/Stay
        // Use NavMesh.SamplePosition so the destination is always on the mesh
        if (NavMesh.SamplePosition(_player.position, out NavMeshHit navHit, 3f, NavMesh.AllAreas))
            _agent.SetDestination(navHit.position);
        else
            _agent.SetDestination(_player.position);
    }

    // ── Hit detection (steal on contact) ─────────────────────────────────────

    // Called when player walks into the wolf's trigger sphere OR wolf runs into player
    void OnTriggerEnter(Collider other)
    {
        if (_state == State.Scared) return;
        if (!other.CompareTag(Tags.Player)) return;
        AttemptSteal();
    }

    // Also fires while player stays inside the trigger (handles slow overlap)
    void OnTriggerStay(Collider other)
    {
        if (_state == State.Scared) return;
        if (!other.CompareTag(Tags.Player)) return;
        AttemptSteal();
    }

    void AttemptSteal()
    {
        if (Time.time - _lastStealTime < _stealCooldown) return;
        if (_playerCtrl == null) return;

        int wood  = _playerCtrl.currentElementsWood.Count;
        int fish  = _playerCtrl.currentElementsFish.Count;
        if (wood + fish == 0) return;   // nothing to steal

        _lastStealTime   = Time.time;
        _agent.isStopped = true;

        int amount = Mathf.Min(_stealAmount, wood + fish);
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

        // Squash-and-stretch pop, then run home
        LeanTween.cancel(gameObject);
        LeanTween.scale(gameObject,
            new Vector3(StealPopScale, 1f / StealPopScale, StealPopScale), StealPopTime)
            .setEaseOutQuad()
            .setOnComplete(() =>
            {
                LeanTween.scale(gameObject, Vector3.one, StealPopTime * 2f).setEaseOutBack();
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

    void SyncAnimation()
    {
        if (_animator == null) return;
        bool isMoving = _agent.velocity.magnitude > 0.15f;
        if (isMoving == _wasMoving) return;
        _animator.CrossFade(isMoving ? AnimRun : AnimIdle, 0.15f);
        _wasMoving = isMoving;
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
