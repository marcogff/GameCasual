using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject _enemyPrefab;
    [SerializeField] private int        _count            = 2;
    [SerializeField] private float      _spawnRadius      = 4f;

    // If the wolf still floats or clips after the raycast, tweak this in the
    // Inspector (positive = raise, negative = sink). Usually 0 is fine.
    [SerializeField] private float      _spawnHeightOffset = 0f;

    // Only hit these layers when looking for the ground. Defaults to Everything.
    [SerializeField] private LayerMask  _groundMask       = Physics.DefaultRaycastLayers;

    void Start()
    {
        for (int i = 0; i < _count; i++)
            SpawnEnemy();
    }

    void SpawnEnemy()
    {
        Vector2 circle = Random.insideUnitCircle * _spawnRadius;
        Vector3 rawPos = transform.position + new Vector3(circle.x, 0f, circle.y);

        // Cast from well above the spawn point straight down to find the real ground Y.
        // This means the spawner can be placed anywhere in the scene — it doesn't need
        // to sit exactly on the floor.
        Vector3 spawnPos = rawPos;
        Vector3 origin   = rawPos + Vector3.up * 50f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 100f, _groundMask))
            spawnPos.y = hit.point.y + _spawnHeightOffset;

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
