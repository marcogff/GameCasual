using UnityEngine;

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
        Vector2 circle  = Random.insideUnitCircle * _spawnRadius;
        Vector3 pos     = transform.position + new Vector3(circle.x, 0f, circle.y);
        GameObject enemy = Instantiate(_enemyPrefab, pos, Quaternion.identity);

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
