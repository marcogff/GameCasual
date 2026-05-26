using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject _enemyPrefab;
    [SerializeField] private int        _count       = 2;
    [SerializeField] private float      _spawnRadius = 4f;

    void Start()
    {
        for (int i = 0; i < _count; i++)
            SpawnEnemy();
    }

    void SpawnEnemy()
    {
        // Find a valid NavMesh position near the spawner.
        // This replaces the old Physics.Raycast approach — NavMesh.SamplePosition
        // guarantees the wolf spawns on a walkable surface and won't float or clip.
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
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, _spawnRadius);
    }
#endif
}
