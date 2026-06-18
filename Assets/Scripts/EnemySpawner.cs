using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject _enemyPrefab;
    [SerializeField] private int        _count       = 2;
    [SerializeField] private float      _spawnRadius = 4f;

    private readonly List<GameObject> _alive = new List<GameObject>();

    // ── Networking ──────────────────────────────────────────────────────────
    // Solo: NetworkManager isn't listening → we spawn locally as before.
    // Multiplayer: only the SERVER spawns wolves; clients receive them as replicas
    // via NetworkObject. (Phase 4 — needs NetworkObject + NetworkTransform on the
    // Wolf prefab and the prefab registered with NetworkManager.)
    private bool Networked => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
    private bool ShouldSpawn => !Networked || NetworkManager.Singleton.IsServer;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Start()
    {
        if (!ShouldSpawn) return;
        for (int i = 0; i < _count; i++)
            SpawnEnemy();
    }

    void Update()
    {
        if (!ShouldSpawn) return;

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

        // In a networked session, replicate the wolf to all clients.
        if (Networked)
        {
            var netObj = enemy.GetComponent<NetworkObject>();
            if (netObj != null) netObj.Spawn(true);
            else Debug.LogWarning("[EnemySpawner] Wolf prefab has no NetworkObject — " +
                                  "it won't be visible to other players. Add NetworkObject + " +
                                  "NetworkTransform to the Wolf prefab for multiplayer.");
        }

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
