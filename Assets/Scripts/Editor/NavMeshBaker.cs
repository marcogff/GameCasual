#if UNITY_EDITOR
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;            // NavMeshCollectGeometry

/// <summary>
/// Tools → Bake NavMesh
///
/// Unity 6's AI Navigation package moved baking OUT of the old Navigation window
/// (which now only shows Agents/Areas — no Bake tab). Baking is done through a
/// NavMeshSurface component instead.
///
/// This tool finds or creates a scene-wide NavMeshSurface and bakes it in one click,
/// so the wolf has a NavMesh to walk on. Re-run it whenever the walkable layout
/// changes (e.g. after adding terrain).
/// </summary>
public static class NavMeshBaker
{
    [MenuItem("Tools/Bake NavMesh")]
    static void Bake()
    {
        var surface = Object.FindFirstObjectByType<NavMeshSurface>();

        if (surface == null)
        {
            var go = new GameObject("NavMeshSurface");
            surface = go.AddComponent<NavMeshSurface>();
            // Bake the whole scene from render meshes so we don't depend on colliders.
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry    = NavMeshCollectGeometry.RenderMeshes;
            Undo.RegisterCreatedObjectUndo(go, "Create NavMeshSurface");
            Debug.Log("[NavMeshBaker] Created a scene-wide NavMeshSurface.");
        }

        surface.BuildNavMesh();

        EditorUtility.SetDirty(surface);
        EditorSceneManager.MarkSceneDirty(surface.gameObject.scene);

        Debug.Log("[NavMeshBaker] NavMesh baked. Press Ctrl+S to save the scene so it persists.");
        EditorUtility.DisplayDialog(
            "NavMesh Baked",
            "The NavMesh was rebuilt over the whole scene.\n\n" +
            "• Select the new 'NavMeshSurface' object and enable Gizmos to see the blue\n" +
            "  walkable overlay in the Scene view.\n" +
            "• If the wolf still can't reach some areas, those meshes are either not\n" +
            "  connected (gaps/steps too high) or were added after baking — re-run this.\n" +
            "• Land the player unlocks at runtime is NOT in the bake.\n\n" +
            "Remember to save the scene (Ctrl+S).",
            "OK");
    }
}
#endif
