using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

/// <summary>
/// Menu: Tools → Setup Wolf Animator
///
/// Root cause of "(Missing!)" bones and silent animation failures:
///   1. The Animator had no Avatar assigned (m_Avatar = null).
///      Generic rigs REQUIRE an Avatar to map animation curves to bones.
///   2. Previous setup runs accidentally used idle.fbx / run.fbx clips
///      which belong to a different rig ("Root") — not the wolf's rig.
///
/// This tool fixes both in one click:
///   • Loads the Avatar embedded inside wolf.fbx and assigns it
///   • Uses ONLY clips from wolf.fbx (wolf_rig|idle, wolf_rig|running)
///   • Adds Speed (Float) parameter + transitions to the controller
///   • Saves the Wolf prefab
///
/// Run  Tools → List Wolf Clips  first to see every clip name available.
/// </summary>
public static class WolfAnimatorSetup
{
    // ── Asset paths ──────────────────────────────────────────────────────────
    private const string WolfFbxPath    = "Assets/Art/Models/Wolf/source/wolf.fbx";
    private const string ControllerPath = "Assets/Art/Animations 1/Wolf/WolfAnimator.controller";
    private const string WolfPrefabPath = "Assets/Prefabs/Wolf.prefab";
    public  const string SpeedParam     = "Speed";

    // ── Diagnostic ───────────────────────────────────────────────────────────
    [MenuItem("Tools/List Wolf Clips")]
    public static void ListClips()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"=== Clips in '{WolfFbxPath}' ===");
        foreach (var c in GetAllClips(WolfFbxPath))
            sb.AppendLine($"  '{c.name}'  {c.length:F2}s  loop={c.isLooping}");

        // Also report Avatar
        var avatar = GetAvatar(WolfFbxPath);
        sb.AppendLine(avatar != null
            ? $"\nAvatar found: '{avatar.name}'"
            : "\nNo Avatar found in wolf.fbx — rig type may not be Generic/Humanoid.");

        Debug.Log(sb.ToString());
    }

    // ── Main setup ────────────────────────────────────────────────────────────
    [MenuItem("Tools/Setup Wolf Animator")]
    public static void Setup()
    {
        // ── 1. Load animation clips from wolf.fbx only ───────────────────────
        // wolf.fbx clips are named "wolf_rig|idle", "wolf_rig|running", etc.
        // Using clips from any other FBX causes "(Missing!)" because the bone
        // paths don't match the wolf model's skeleton.
        var idleClip = FindClip(WolfFbxPath, "idle",    "Idle",    "IDLE",
                                             "stand",   "Stand");
        var runClip  = FindClip(WolfFbxPath, "running", "Running", "run",
                                             "Run",     "RUN",     "walk", "Walk");

        if (idleClip == null || runClip == null)
        {
            Debug.LogError(
                "[WolfAnimatorSetup] Could not find idle or run clips in wolf.fbx.\n" +
                "Run  Tools → List Wolf Clips  to see the exact names, then add them\n" +
                "to the keyword list inside WolfAnimatorSetup.cs → Setup().");
            return;
        }

        Debug.Log($"[WolfAnimatorSetup] Clips resolved:\n" +
                  $"  Idle → '{idleClip.name}'\n" +
                  $"  Run  → '{runClip.name}'");

        // ── 2. Load Avatar from wolf.fbx ─────────────────────────────────────
        // Generic rigs need the Avatar to map animation curves to bones.
        // Without it the Animator silently plays nothing.
        var avatar = GetAvatar(WolfFbxPath);
        if (avatar == null)
            Debug.LogWarning("[WolfAnimatorSetup] No Avatar found in wolf.fbx. " +
                             "Animations may still not play. Open the wolf.fbx importer, " +
                             "set Rig → Animation Type → Generic, click Apply.");

        // ── 3. Configure controller ──────────────────────────────────────────
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            Debug.LogError($"[WolfAnimatorSetup] Controller not found at '{ControllerPath}'.");
            return;
        }

        // Clear everything and rebuild from scratch
        while (controller.parameters.Length > 0)
            controller.RemoveParameter(0);
        controller.AddParameter(SpeedParam, AnimatorControllerParameterType.Float);

        var sm = controller.layers[0].stateMachine;
        foreach (var s in sm.states.ToArray())
            sm.RemoveState(s.state);

        var idleState   = sm.AddState("Idle");
        idleState.motion = idleClip;
        sm.defaultState  = idleState;

        var runState    = sm.AddState("Run");
        runState.motion  = runClip;

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

        // ── 4. Assign controller AND Avatar to Wolf prefab ───────────────────
        var wolfPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WolfPrefabPath);
        if (wolfPrefab == null)
        {
            Debug.LogWarning($"[WolfAnimatorSetup] Prefab not found at '{WolfPrefabPath}'. " +
                             "Assign the controller and Avatar manually.");
        }
        else
        {
            bool changed = false;
            foreach (var anim in wolfPrefab.GetComponentsInChildren<Animator>(true))
            {
                anim.runtimeAnimatorController = controller;
                if (avatar != null)
                    anim.avatar = avatar;
                EditorUtility.SetDirty(anim);
                Debug.Log($"[WolfAnimatorSetup] '{anim.gameObject.name}' → " +
                          $"controller='{controller.name}'  avatar='{avatar?.name ?? "null"}'");
                changed = true;
            }

            if (changed)
                PrefabUtility.SavePrefabAsset(wolfPrefab);
            else
                Debug.LogWarning("[WolfAnimatorSetup] No Animator component found in Wolf prefab.");
        }

        AssetDatabase.Refresh();
        Debug.Log("[WolfAnimatorSetup] Done — hit Play to test.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static AnimationClip FindClip(string fbxPath, params string[] keywords)
        => GetAllClips(fbxPath).FirstOrDefault(c => keywords.Any(k => c.name.Contains(k)));

    static AnimationClip[] GetAllClips(string fbxPath)
        => AssetDatabase.LoadAllAssetsAtPath(fbxPath)
            .OfType<AnimationClip>()
            .Where(c => !c.name.StartsWith("__preview__"))
            .ToArray();

    static Avatar GetAvatar(string fbxPath)
        => AssetDatabase.LoadAllAssetsAtPath(fbxPath)
            .OfType<Avatar>()
            .FirstOrDefault();
}
