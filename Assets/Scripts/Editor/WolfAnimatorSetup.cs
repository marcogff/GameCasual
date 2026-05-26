using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

/// <summary>
/// One-click setup for the wolf Animator Controller.
/// Menu: Tools → Setup Wolf Animator
///
/// What it does:
///   1. Clears any existing states / parameters from WolfAnimator.controller
///   2. Adds a float parameter called "Speed"
///   3. Creates an Idle state (idle.fbx clip) and a Run state (run.fbx clip)
///   4. Wires transitions: Idle→Run when Speed > 0.1, Run→Idle when Speed < 0.1
///   5. Saves the asset so Unity picks it up immediately
/// </summary>
public static class WolfAnimatorSetup
{
    private const string ControllerPath = "Assets/Art/Animations 1/Wolf/WolfAnimator.controller";
    private const string IdleFbxPath    = "Assets/Art/Animations 1/idle.fbx";
    private const string RunFbxPath     = "Assets/Art/Animations 1/run.fbx";
    public  const string SpeedParam     = "Speed";  // must match Enemy._runParam default

    [MenuItem("Tools/Setup Wolf Animator")]
    public static void Setup()
    {
        // ── Load assets ─────────────────────────────────────────────────────
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            Debug.LogError($"[WolfAnimatorSetup] Controller not found at '{ControllerPath}'. " +
                           "Check the path matches your project layout.");
            return;
        }

        var idleClip = GetFirstClip(IdleFbxPath);
        var runClip  = GetFirstClip(RunFbxPath);

        if (idleClip == null) Debug.LogWarning($"[WolfAnimatorSetup] No clip found in '{IdleFbxPath}'");
        if (runClip  == null) Debug.LogWarning($"[WolfAnimatorSetup] No clip found in '{RunFbxPath}'");

        // ── Parameters ──────────────────────────────────────────────────────
        // Wipe all existing parameters and start fresh with just Speed (Float).
        while (controller.parameters.Length > 0)
            controller.RemoveParameter(0);

        controller.AddParameter(SpeedParam, AnimatorControllerParameterType.Float);

        // ── States ──────────────────────────────────────────────────────────
        // Ensure there is at least one layer (controllers always have Base Layer).
        if (controller.layers.Length == 0)
        {
            Debug.LogError("[WolfAnimatorSetup] Controller has no layers — cannot continue.");
            return;
        }

        AnimatorStateMachine sm = controller.layers[0].stateMachine;

        // Remove every existing state so we don't accumulate duplicates on repeated runs.
        foreach (var s in sm.states.ToArray())
            sm.RemoveState(s.state);

        // Idle
        AnimatorState idleState = sm.AddState("Idle");
        idleState.motion  = idleClip;
        sm.defaultState   = idleState;

        // Run
        AnimatorState runState = sm.AddState("Run");
        runState.motion = runClip;

        // ── Transitions ─────────────────────────────────────────────────────
        AnimatorStateTransition toRun = idleState.AddTransition(runState);
        toRun.AddCondition(AnimatorConditionMode.Greater, 0.1f, SpeedParam);
        toRun.hasExitTime = false;
        toRun.duration    = 0.15f;  // blend time in seconds

        AnimatorStateTransition toIdle = runState.AddTransition(idleState);
        toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, SpeedParam);
        toIdle.hasExitTime = false;
        toIdle.duration    = 0.15f;

        // ── Save ────────────────────────────────────────────────────────────
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[WolfAnimatorSetup] Done!\n" +
                  $"  Idle  → clip: '{idleClip?.name ?? "MISSING"}'\n" +
                  $"  Run   → clip: '{runClip?.name  ?? "MISSING"}'\n" +
                  $"  Param → '{SpeedParam}' (Float)\n" +
                  $"  Enemy._runParam must be set to '{SpeedParam}' in the Inspector.");
    }

    // Returns the first real AnimationClip inside an FBX.
    // Unity embeds clips as sub-assets; "__preview__" clips are editor-only previews.
    static AnimationClip GetFirstClip(string fbxPath)
    {
        return AssetDatabase.LoadAllAssetsAtPath(fbxPath)
            .OfType<AnimationClip>()
            .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
    }
}
