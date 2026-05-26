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
    [SerializeField] private float _detectionRange = 8f;
    [SerializeField] private float _loseRange      = 13f;
    [SerializeField] private float _stealRange     = 1.5f;
    [SerializeField] private float _scareRange     = 3.5f;

    [Header("Stealing")]
    [SerializeField] private int   _stealAmount    = 5;
    [SerializeField] private float _stealCooldown  = 4f;
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

        // Tune agent so it feels responsive
        _agent.speed         = _wanderSpeed;
        _agent.acceleration  = 12f;
        _agent.angularSpeed  = 360f;
        _agent.stoppingDistance = 0.3f;

        var playerGO = GameObject.FindWithTag(Tags.Player);
        if (playerGO != null)
        {
            _player     = playerGO.transform;
            _playerCtrl = playerGO.GetComponent<PlayerController>();
        }
        else
        {
            Debug.LogWarning("[Enemy] Player tag not set — select the player GameObject → Inspector → Tag → Player.");
        }

        if (!_agent.isOnNavMesh)
        {
            Debug.LogError("[Enemy] Wolf is NOT on the NavMesh!\n" +
                           "1. Select your terrain/ground → Inspector → Static dropdown → tick Navigation Static\n" +
                           "2. Window → AI → Navigation → Bake tab → click Bake\n" +
                           "3. Blue overlay appears on walkable surfaces — wolf will work after that.");
            return;   // don't call PickWanderTarget — agent can't be used yet
        }

        PickWanderTarget();
    }

    // ── Update ───────────────────────────────────────────────────────────────

    void Update()
    {
        if (_player == null || _playerCtrl == null) return;
        if (!_agent.isOnNavMesh) return;   // NavMesh not baked yet — wait silently

        switch (_state)
        {
            case State.Wander:    UpdateWander();    break;
            case State.Chase:     UpdateChase();     break;
            case State.Scared:    /* coroutine */    break;
            case State.Returning: UpdateReturning(); break;
        }

        SyncAnimation();
    }

    // ── States ───────────────────────────────────────────────────────────────

    void UpdateWander()
    {
        float dist = Vector3.Distance(transform.position, _player.position);

        // Spot the player if they're carrying something
        if (dist <= _detectionRange && _playerCtrl.hasMat)
        {
            _state       = State.Chase;
            _agent.speed = _chaseSpeed;
            return;
        }

        // Arrived — pick the next random stroll point
        if (!_agent.pathPending && _agent.remainingDistance < 0.5f)
            PickWanderTarget();
    }

    void PickWanderTarget()
    {
        Vector3 origin = spawnPoint != null ? spawnPoint.position : transform.position;

        // Try up to 8 times to land on a valid NavMesh position
        for (int i = 0; i < 8; i++)
        {
            Vector2 circle   = Random.insideUnitCircle * _wanderRadius;
            Vector3 candidate = origin + new Vector3(circle.x, 0f, circle.y);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            {
                _agent.SetDestination(hit.position);
                return;
            }
        }

        // Absolute fallback — just return to spawn
        _agent.SetDestination(origin);
    }

    void UpdateChase()
    {
        float dist = Vector3.Distance(transform.position, _player.position);

        // Give up if player put everything down or ran too far
        if (!_playerCtrl.hasMat || dist > _loseRange)
        {
            _state       = State.Wander;
            _agent.speed = _wanderSpeed;
            PickWanderTarget();
            return;
        }

        // Flee if the player is charging straight at us
        if (dist < _scareRange && IsPlayerChargingAtMe())
        {
            StartCoroutine(ScaredRoutine());
            return;
        }

        // Steal when close enough
        if (dist <= _stealRange && Time.time - _lastStealTime >= _stealCooldown)
        {
            TrySteal();
            return;
        }

        _agent.SetDestination(_player.position);
    }

    bool IsPlayerChargingAtMe()
    {
        // targetTransform is the ghost CC that holds the real player velocity
        Vector3 vel = _playerCtrl.targetTransform.velocity;
        if (vel.magnitude < 2.5f) return false;
        Vector3 toEnemy = (transform.position - _player.position).normalized;
        return Vector3.Dot(vel.normalized, toEnemy) > 0.7f;
    }

    void TrySteal()
    {
        _lastStealTime    = Time.time;
        _agent.isStopped  = true;  // freeze while celebrating

        int wood  = _playerCtrl.currentElementsWood.Count;
        int fish  = _playerCtrl.currentElementsFish.Count;
        int total = wood + fish;

        if (total == 0) { StartReturning(); return; }

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

        // Squash-and-stretch then run home
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

    void StartReturning()
    {
        _state            = State.Returning;
        _agent.isStopped  = false;
        _agent.speed      = _wanderSpeed;

        if (spawnPoint != null)
            _agent.SetDestination(spawnPoint.position);
    }

    IEnumerator ScaredRoutine()
    {
        _state       = State.Scared;
        _agent.speed = _scaredSpeed;

        float end = Time.time + _scaredDuration;
        while (Time.time < end)
        {
            // Recalculate flee direction every half second
            Vector3 fleeDir    = (transform.position - _player.position).normalized;
            Vector3 fleeTarget = transform.position + fleeDir * 6f;

            if (NavMesh.SamplePosition(fleeTarget, out NavMeshHit hit, 4f, NavMesh.AllAreas))
                _agent.SetDestination(hit.position);

            yield return new WaitForSeconds(0.5f);
        }

        StartReturning();
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

        // NavMeshAgent.velocity is reliable — no CC issues
        bool isMoving = _agent.velocity.magnitude > 0.15f;
        if (isMoving == _wasMoving) return;

        _animator.CrossFade(isMoving ? AnimRun : AnimIdle, 0.15f);
        _wasMoving = isMoving;
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
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _loseRange);
    }
#endif
}
