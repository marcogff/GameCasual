#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

/// <summary>
/// Tools → Setup Post-Processing
///
/// One-click Post-Processing v2 setup for the Built-in pipeline (low risk, fully
/// reversible — delete the PostFX object + profile to undo). It:
///   1. Adds a PostProcessLayer to the main camera and assigns the package's
///      PostProcessResources (found via AssetDatabase — the part that can't be
///      done reliably at runtime).
///   2. Creates a profile asset with Bloom + Color Grading + Vignette.
///   3. Creates a global PostProcessVolume referencing that profile.
///
/// Run once after the com.unity.postprocessing package finishes importing.
/// </summary>
public static class PostFXSetup
{
    private const string ProfilePath = "Assets/Settings/PostFXProfile.asset";

    [MenuItem("Tools/Setup Post-Processing")]
    static void Setup()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            EditorUtility.DisplayDialog("Post-Processing",
                "No camera tagged 'MainCamera' was found. Tag your gameplay camera as " +
                "MainCamera and run this again.", "OK");
            return;
        }

        // ── 1. Camera layer + resources ───────────────────────────────────────
        var layer = cam.GetComponent<PostProcessLayer>();
        if (layer == null) layer = cam.gameObject.AddComponent<PostProcessLayer>();

        var resources = FindResources();
        if (resources == null)
        {
            EditorUtility.DisplayDialog("Post-Processing",
                "Couldn't find PostProcessResources. Make sure the Post Processing package " +
                "finished importing, then run this again.", "OK");
            return;
        }
        layer.Init(resources);
        layer.volumeTrigger    = cam.transform;
        layer.volumeLayer      = 1; // "Default" layer mask (layer 0)
        layer.antialiasingMode = PostProcessLayer.Antialiasing.FastApproximateAntialiasing;

        // ── 2. Profile asset ───────────────────────────────────────────────────
        Directory.CreateDirectory("Assets/Settings");
        var profile = AssetDatabase.LoadAssetAtPath<PostProcessProfile>(ProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<PostProcessProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);
        }

        if (!profile.HasSettings<Bloom>())
        {
            var bloom = profile.AddSettings<Bloom>();
            bloom.enabled.Override(true);
            bloom.intensity.Override(1.1f);
            bloom.threshold.Override(1.05f);
            bloom.softKnee.Override(0.6f);
        }
        if (!profile.HasSettings<ColorGrading>())
        {
            var cg = profile.AddSettings<ColorGrading>();
            cg.enabled.Override(true);
            cg.postExposure.Override(0.08f);
            cg.contrast.Override(8f);
            cg.saturation.Override(12f);
        }
        if (!profile.HasSettings<Vignette>())
        {
            var vig = profile.AddSettings<Vignette>();
            vig.enabled.Override(true);
            vig.intensity.Override(0.28f);
            vig.smoothness.Override(0.4f);
        }
        EditorUtility.SetDirty(profile);

        // ── 3. Global volume ─────────────────────────────────────────────────────
        var volGO = GameObject.Find("PostFX");
        if (volGO == null) volGO = new GameObject("PostFX");
        volGO.layer = 0; // Default
        var vol = volGO.GetComponent<PostProcessVolume>();
        if (vol == null) vol = volGO.AddComponent<PostProcessVolume>();
        vol.isGlobal = true;
        vol.priority = 1f;
        vol.profile  = profile;

        AssetDatabase.SaveAssets();
        EditorUtility.SetDirty(cam.gameObject);
        EditorSceneManager.MarkSceneDirty(cam.gameObject.scene);

        EditorUtility.DisplayDialog("Post-Processing Ready",
            "Added Bloom + Color Grading + Vignette.\n\n" +
            "• Camera: PostProcessLayer (FXAA) on '" + cam.name + "'\n" +
            "• Scene: global 'PostFX' volume → Assets/Settings/PostFXProfile.asset\n\n" +
            "Tweak the look by selecting PostFXProfile in the Project window.\n" +
            "To remove: delete the PostFX object, the profile, and the camera's " +
            "PostProcessLayer.\n\nSave the scene (Ctrl+S).", "OK");
    }

    static PostProcessResources FindResources()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:PostProcessResources"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var res  = AssetDatabase.LoadAssetAtPath<PostProcessResources>(path);
            if (res != null) return res;
        }
        return null;
    }
}
#endif
