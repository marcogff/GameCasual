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
            surface.collectObjects = CollectObjects.All;
            Undo.RegisterCreatedObjectUndo(go, "Create NavMeshSurface");
            Debug.Log("[NavMeshBaker] Created a scene-wide NavMeshSurface.");
        }

        // Bake from PHYSICS COLLIDERS, not render meshes. This project's terrain is
        // ProBuilder geometry, whose render meshes have a vertex layout the NavMesh
        // builder rejects ("pb_Mesh... has invalid vertex data and will be skipped").
        // The ground already has colliders (the player walks on them), so colliders
        // are both valid and exactly the surface we want to walk on.
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;

        surface.BuildNavMesh();

        EditorUtility.SetDirty(surface);
        EditorSceneManager.MarkSceneDirty(surface.gameObject.scene);

        Debug.Log("[NavMeshBaker] NavMesh baked. Press Ctrl+S to save the scene so it persists.");
        EditorUtility.DisplayDialog(
            "NavMesh Baked",
            "The NavMesh was rebuilt from the scene's PHYSICS COLLIDERS.\n\n" +
            "• Select the 'NavMeshSurface' object and enable Gizmos to see the blue\n" +
            "  walkable overlay in the Scene view.\n" +
            "• Walkable ground must have a Collider (MeshCollider/BoxCollider). The\n" +
            "  ProBuilder render meshes are intentionally ignored (invalid for NavMesh).\n" +
            "• If an area has no blue overlay, give that object a Collider and re-run.\n" +
            "• Land the player unlocks at runtime is NOT in the bake.\n\n" +
            "Remember to save the scene (Ctrl+S).",
            "OK");
    }
}
#endif
