using UnityEngine;

/// <summary>
/// Self-spawning, gentle daylight atmosphere cycle.
///
/// Finds the scene's directional light and smoothly arcs it between a warm low
/// "golden hour" and a bright high "midday" and back, also drifting the ambient
/// colour. It deliberately NEVER goes dark — this is mood, not a survival night —
/// so it can't make the game unplayable.
///
/// Self-contained: auto-spawns after the scene loads. To disable, delete this file.
/// Tune the look with the constants below.
/// </summary>
public class DayNightCycle : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoSpawn()
    {
        if (FindFirstObjectByType<DayNightCycle>() != null) return;
        new GameObject("DayNightCycle").AddComponent<DayNightCycle>();
    }

    // ── Tuning ────────────────────────────────────────────────────────────────
    private const float CycleSeconds = 110f;          // full warm→bright→warm loop

    private const float PitchLow  = 22f;              // sun near horizon (warm)
    private const float PitchHigh = 62f;              // sun high (midday)

    private static readonly Color WarmLight   = new Color(1f, 0.82f, 0.60f);
    private static readonly Color BrightLight = new Color(1f, 0.97f, 0.90f);
    private const float IntensityLow  = 0.80f;
    private const float IntensityHigh = 1.15f;

    private static readonly Color WarmAmbient   = new Color(0.42f, 0.40f, 0.46f);
    private static readonly Color BrightAmbient = new Color(0.62f, 0.64f, 0.62f);

    // ── State ───────────────────────────────────────────────────────────────────
    private Light _sun;
    private float _baseYaw;

    void Start()
    {
        _sun = FindSun();
        if (_sun == null)
        {
            // Nothing to drive — remove ourselves quietly.
            enabled = false;
            return;
        }
        _baseYaw = _sun.transform.eulerAngles.y;
    }

    void Update()
    {
        if (_sun == null) return;

        // Smooth 0..1..0 over the cycle (sine = no hard turn-arounds)
        float t = Mathf.Sin(Time.time / CycleSeconds * Mathf.PI * 2f) * 0.5f + 0.5f;

        float pitch = Mathf.Lerp(PitchLow, PitchHigh, t);
        _sun.transform.rotation = Quaternion.Euler(pitch, _baseYaw, 0f);

        _sun.color     = Color.Lerp(WarmLight, BrightLight, t);
        _sun.intensity = Mathf.Lerp(IntensityLow, IntensityHigh, t);

        RenderSettings.ambientLight = Color.Lerp(WarmAmbient, BrightAmbient, t);
    }

    static Light FindSun()
    {
        // Prefer RenderSettings.sun, else the first enabled directional light
        if (RenderSettings.sun != null) return RenderSettings.sun;
        foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.type == LightType.Directional && l.enabled) return l;
        return null;
    }
}
