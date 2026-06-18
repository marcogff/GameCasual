using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject _enemyPrefab;
    [SerializeField] private int        _count       = 2;
    [SerializeField] private float      _spawnRadius = 4f;

    private readonly List<GameObject> _alive = new List<GameObject>();

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Start()
    {
        for (int i = 0; i < _count; i++)
            SpawnEnemy();
    }

    void Update()
    {
        // Remove destroyed entries (wolf fell off world, etc.)
        _alive.RemoveAll(e => e == null);

        // Re-spawn to maintain the desired population
        while (_alive.Count < _count)
            SpawnEnemy();
    }

    // ── Spawn ─────────────────────────────────────────────────────────────────

    void SpawnEnemy()
    {
        Vector3 spawnPos = transform.position;

        for (int attempt = 0; attempt < 10; attempt++)
        {
            Vector2 circle    = Random.insideUnitCircle * _spawnRadius;
            Vector3 candidate = transform.position + new Vector3(circle.x, 0f, circle.y);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            {
                spawnPos = hit.position;
                break;
            }
        }

        GameObject enemy = Instantiate(_enemyPrefab, spawnPos, Quaternion.identity);

        var ctrl = enemy.GetComponent<Enemy>();
        if (ctrl != null)
            ctrl.spawnPoint = transform;

        _alive.Add(enemy);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, _spawnRadius);
    }
#endif
}
