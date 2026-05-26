using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

/// <summary>
/// Menu: Tools → Setup Wolf Animator
///
/// Scans wolf.fbx for embedded animation clips (they share the same rig,
/// so bindings are always correct). Auto-detects idle and run clips by name,
/// builds the AnimatorController, and assigns it to the Wolf prefab.
///
/// Also run  Tools → List Wolf Clips  to see every clip available.
/// </summary>
public static class WolfAnimatorSetup
{
    private const string WolfFbxPath     = "Assets/Art/Models/Wolf/source/wolf.fbx";
    private const string ControllerPath  = "Assets/Art/Animations 1/Wolf/WolfAnimator.controller";
    private const string WolfPrefabPath  = "Assets/Prefabs/Wolf.prefab";
    public  const string SpeedParam      = "Speed";

    // ── Diagnostic: list every clip in wolf.fbx ──────────────────────────────
    [MenuItem("Tools/List Wolf Clips")]
    public static void ListClips()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[WolfAnimator] Clips inside '{WolfFbxPath}':");

        var clips = GetAllClips(WolfFbxPath);
        if (clips.Length == 0)
        {
            sb.AppendLine("  (none — the FBX has no embedded animations)");
        }
        else
        {
            foreach (var c in clips)
                sb.AppendLine($"  '{c.name}'  length={c.length:F2}s  loop={c.isLooping}");
        }

        // Also check the separate animation FBX files as fallback info
        string[] extraFbx =
        {
            "Assets/Art/Animations 1/idle.fbx",
            "Assets/Art/Animations 1/run.fbx",
            "Assets/Art/Animations 1/jump.fbx",
        };
        foreach (var path in extraFbx)
        {
            var extra = GetAllClips(path);
            if (extra.Length > 0)
            {
                sb.AppendLine($"\n[WolfAnimator] Clips inside '{path}':");
                foreach (var c in extra)
                    sb.AppendLine($"  '{c.name}'  length={c.length:F2}s  loop={c.isLooping}");
            }
        }

        Debug.Log(sb.ToString());
    }

    // ── Main setup ────────────────────────────────────────────────────────────
    [MenuItem("Tools/Setup Wolf Animator")]
    public static void Setup()
    {
        // ── Find idle and run clips ──────────────────────────────────────────
        // Priority: wolf.fbx (same rig = correct bindings)
        // Fallback:  separate FBX files (may have rig mismatch)
        AnimationClip idleClip = FindClip(WolfFbxPath, "idle", "Idle", "IDLE", "stand", "Stand");
        AnimationClip runClip  = FindClip(WolfFbxPath, "run",  "Run",  "RUN",  "walk", "Walk");

        string idleSource = WolfFbxPath;
        string runSource  = WolfFbxPath;

        if (idleClip == null)
        {
            idleClip   = GetFirstClip("Assets/Art/Animations 1/idle.fbx");
            idleSource = "idle.fbx";
        }
        if (runClip == null)
        {
            runClip   = GetFirstClip("Assets/Art/Animations 1/run.fbx");
            runSource = "run.fbx";
        }

        if (idleClip == null || runClip == null)
        {
            Debug.LogError(
                "[WolfAnimatorSetup] Could not find idle or run clips.\n" +
                "Run  Tools → List Wolf Clips  to see what names are available,\n" +
                "then update the name list in FindClip() to match.");
            return;
        }

        Debug.Log($"[WolfAnimatorSetup] Using:\n" +
                  $"  Idle → '{idleClip.name}' from {idleSource}\n" +
                  $"  Run  → '{runClip.name}'  from {runSource}");

        // ── Load / validate controller ───────────────────────────────────────
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            Debug.LogError($"[WolfAnimatorSetup] Controller not found at '{ControllerPath}'.");
            return;
        }

        // ── Parameters ──────────────────────────────────────────────────────
        while (controller.parameters.Length > 0)
            controller.RemoveParameter(0);
        controller.AddParameter(SpeedParam, AnimatorControllerParameterType.Float);

        // ── States ──────────────────────────────────────────────────────────
        var sm = controller.layers[0].stateMachine;
        foreach (var s in sm.states.ToArray())
            sm.RemoveState(s.state);

        var idleState   = sm.AddState("Idle");
        idleState.motion = idleClip;
        sm.defaultState  = idleState;

        var runState    = sm.AddState("Run");
        runState.motion  = runClip;

        // ── Transitions ──────────────────────────────────────────────────────
        var toRun = idleState.AddTransition(runState);
        toRun.AddCondition(AnimatorConditionMode.Greater, 0.1f, SpeedParam);
        toRun.hasExitTime = false;
        toRun.duration    = 0.15f;

        var toIdle = runState.AddTransition(idleState);
        toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, SpeedParam);
        toIdle.hasExitTime = false;
        toIdle.duration    = 0.15f;

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        // ── Assign to Wolf prefab ────────────────────────────────────────────
        var wolfPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WolfPrefabPath);
        if (wolfPrefab == null)
        {
            Debug.LogWarning($"[WolfAnimatorSetup] Prefab not found at '{WolfPrefabPath}'. " +
                             "Assign WolfAnimator.controller to the Animator manually.");
        }
        else
        {
            foreach (var anim in wolfPrefab.GetComponentsInChildren<Animator>(true))
            {
                anim.runtimeAnimatorController = controller;
                EditorUtility.SetDirty(anim);
                Debug.Log($"[WolfAnimatorSetup] Controller assigned to '{anim.gameObject.name}'.");
            }
            PrefabUtility.SavePrefabAsset(wolfPrefab);
        }

        AssetDatabase.Refresh();
        Debug.Log("[WolfAnimatorSetup] Done — hit Play to test.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    // Returns the first clip whose name contains any of the keywords (case-sensitive).
    static AnimationClip FindClip(string fbxPath, params string[] keywords)
    {
        return GetAllClips(fbxPath)
            .FirstOrDefault(c => keywords.Any(k => c.name.Contains(k)));
    }

    // Returns every real (non-preview) AnimationClip embedded in an FBX.
    static AnimationClip[] GetAllClips(string fbxPath)
    {
        return AssetDatabase.LoadAllAssetsAtPath(fbxPath)
            .OfType<AnimationClip>()
            .Where(c => !c.name.StartsWith("__preview__"))
            .ToArray();
    }

    // Returns the first real clip in an FBX regardless of name.
    static AnimationClip GetFirstClip(string fbxPath)
    {
        return GetAllClips(fbxPath).FirstOrDefault();
    }
}
