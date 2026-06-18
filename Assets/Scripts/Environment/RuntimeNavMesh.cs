using Unity.AI.Navigation;
using UnityEngine;

/// <summary>
/// Rebuilds the scene's NavMeshSurface at runtime so the wolf can chase the player
/// onto land that is unlocked DURING play (MaterialsData.DeployLand). Without this,
/// freshly-built land has no NavMesh and the wolf can't follow there.
///
/// Lazy-singleton (created on first use). Requires a NavMeshSurface in the scene
/// (create one with Tools → Bake NavMesh). Rebakes are debounced so several lands
/// unlocking together trigger a single rebuild.
///
/// Note: only geometry with colliders is included (the surface bakes from physics
/// colliders), so unlocked-land prefabs need a Collider to become walkable.
/// </summary>
public class RuntimeNavMesh : MonoBehaviour
{
    private static RuntimeNavMesh _instance;
    public static RuntimeNavMesh Instance
    {
        get
        {
            if (_instance == null)
                _instance = new GameObject("RuntimeNavMesh").AddComponent<RuntimeNavMesh>();
            return _instance;
        }
    }

    private NavMeshSurface _surface;
    private bool _pending;
    private bool _warnedMissing;

    private const float DebounceSeconds = 0.6f;

    void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        _surface  = FindFirstObjectByType<NavMeshSurface>();
    }

    /// <summary>Schedules a single NavMesh rebuild shortly after being called.</summary>
    public void RequestRebake()
    {
        if (_surface == null) _surface = FindFirstObjectByType<NavMeshSurface>();
        if (_surface == null)
        {
            if (!_warnedMissing)
            {
                _warnedMissing = true;
                Debug.LogWarning("[RuntimeNavMesh] No NavMeshSurface in scene — " +
                                 "run Tools → Bake NavMesh once so unlocked land can be re-baked at runtime.");
            }
            return;
        }

        if (_pending) return;
        _pending = true;
        Invoke(nameof(DoRebake), DebounceSeconds);
    }

    void DoRebake()
    {
        _pending = false;
        if (_surface == null) return;
        _surface.BuildNavMesh(); // runtime rebuild over the whole surface
    }
}
