using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

/// <summary>
/// One-click setup for the wolf Animator Controller.
/// Menu: Tools → Setup Wolf Animator
///
/// What it does:
///   1. Clears stale states/params from WolfAnimator.controller
///   2. Adds float parameter "Speed"
///   3. Creates Idle (idle.fbx) and Run (run.fbx) states with transitions
///   4. Assigns the configured controller to every Animator found inside
///      the Wolf prefab, regardless of what child object it lives on
///   5. Saves everything so Unity picks it up immediately
/// </summary>
public static class WolfAnimatorSetup
{
    private const string ControllerPath = "Assets/Art/Animations 1/Wolf/WolfAnimator.controller";
    private const string IdleFbxPath    = "Assets/Art/Animations 1/idle.fbx";
    private const string RunFbxPath     = "Assets/Art/Animations 1/run.fbx";
    private const string WolfPrefabPath = "Assets/Prefabs/Wolf.prefab";
    public  const string SpeedParam     = "Speed";

    [MenuItem("Tools/Setup Wolf Animator")]
    public static void Setup()
    {
        // ── Load controller ──────────────────────────────────────────────────
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            Debug.LogError($"[WolfAnimatorSetup] Controller not found at '{ControllerPath}'.");
            return;
        }

        // ── Load animation clips ─────────────────────────────────────────────
        var idleClip = GetFirstClip(IdleFbxPath);
        var runClip  = GetFirstClip(RunFbxPath);

        if (idleClip == null) Debug.LogWarning($"[WolfAnimatorSetup] No clip in '{IdleFbxPath}'");
        if (runClip  == null) Debug.LogWarning($"[WolfAnimatorSetup] No clip in '{RunFbxPath}'");

        // ── Parameters ──────────────────────────────────────────────────────
        while (controller.parameters.Length > 0)
            controller.RemoveParameter(0);
        controller.AddParameter(SpeedParam, AnimatorControllerParameterType.Float);

        // ── States ──────────────────────────────────────────────────────────
        if (controller.layers.Length == 0)
        {
            Debug.LogError("[WolfAnimatorSetup] Controller has no layers.");
            return;
        }

        AnimatorStateMachine sm = controller.layers[0].stateMachine;
        foreach (var s in sm.states.ToArray())
            sm.RemoveState(s.state);

        AnimatorState idleState = sm.AddState("Idle");
        idleState.motion = idleClip;
        sm.defaultState  = idleState;

        AnimatorState runState = sm.AddState("Run");
        runState.motion = runClip;

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

        // ── Assign controller to Wolf prefab ─────────────────────────────────
        // The Animator may live on any child (even one named 'Cube' by the FBX
        // importer). GetComponentsInChildren finds it no matter what it's called.
        var wolfPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WolfPrefabPath);
        if (wolfPrefab == null)
        {
            Debug.LogWarning($"[WolfAnimatorSetup] Wolf prefab not found at '{WolfPrefabPath}'. " +
                             "Drag WolfAnimator.controller onto the Animator component manually.");
        }
        else
        {
            var animators = wolfPrefab.GetComponentsInChildren<Animator>(includeInactive: true);
            if (animators.Length == 0)
            {
                Debug.LogWarning("[WolfAnimatorSetup] No Animator found in Wolf prefab. " +
                                 "Add an Animator component to the wolf mesh and re-run this tool.");
            }
            else
            {
                foreach (var anim in animators)
                {
                    anim.runtimeAnimatorController = controller;
                    EditorUtility.SetDirty(anim);
                    Debug.Log($"[WolfAnimatorSetup] Assigned controller to '{anim.gameObject.name}'.");
                }
                PrefabUtility.SavePrefabAsset(wolfPrefab);
            }
        }

        AssetDatabase.Refresh();

        Debug.Log($"[WolfAnimatorSetup] Complete!\n" +
                  $"  Idle  → '{idleClip?.name ?? "MISSING"}'\n" +
                  $"  Run   → '{runClip?.name  ?? "MISSING"}'\n" +
                  $"  Param → '{SpeedParam}' (Float)\n" +
                  $"  Hit Play — wolf should now idle and run.");
    }

    static AnimationClip GetFirstClip(string fbxPath)
    {
        return AssetDatabase.LoadAllAssetsAtPath(fbxPath)
            .OfType<AnimationClip>()
            .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
    }
}
