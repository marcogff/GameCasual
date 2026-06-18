using UnityEngine;

/// <summary>
/// Self-spawning day/night cycle. Rotates the scene's directional light through a
/// full rise→noon→set→midnight loop and drives sun colour/intensity + ambient so
/// the world actually gets dark at night.
///
/// ─────────── TUNE HERE ───────────
///   CycleSeconds     — length of a FULL day+night loop (bigger = slower).
///   NightSunIntensity / NightAmbient — how dark night is (smaller = darker).
///   StartDayT        — where the cycle begins (0.30 ≈ early afternoon, so you
///                      don't spawn into darkness).
/// ──────────────────────────────────
/// Self-contained — auto-spawns after the scene loads. Delete this file to disable.
/// </summary>
public class DayNightCycle : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoSpawn()
    {
        if (FindFirstObjectByType<DayNightCycle>() != null) return;
        new GameObject("DayNightCycle").AddComponent<DayNightCycle>();
    }

    // ===== TUNE HERE =====
    private const float CycleSeconds = 360f;   // full day+night loop (6 min). Raise to slow down.
    private const float StartDayT    = 0.30f;  // start mid-afternoon (0=sunrise .25=noon .5=sunset .75=midnight)

    // Day look
    private static readonly Color DaySunColor   = new Color(1f, 0.96f, 0.86f);
    private const  float          DaySunIntensity = 1.15f;
    private static readonly Color DayAmbient    = new Color(0.60f, 0.62f, 0.60f);

    // Dusk / dawn warm tint near the horizon
    private static readonly Color DuskColor     = new Color(1f, 0.50f, 0.30f);

    // Night look — make these smaller for a darker night
    private static readonly Color NightSunColor = new Color(0.45f, 0.55f, 0.95f); // moonlight
    private const  float          NightSunIntensity = 0.04f;
    private static readonly Color NightAmbient  = new Color(0.04f, 0.05f, 0.11f); // near-dark blue
    // =====================

    private Light _sun;
    private float _baseYaw;

    void Start()
    {
        _sun = FindSun();
        if (_sun == null) { enabled = false; return; }
        _baseYaw = _sun.transform.eulerAngles.y;
    }

    void Update()
    {
        if (_sun == null) return;

        // Time of day 0..1 (wraps). Scaled time so it pauses with the game.
        float dayT  = Mathf.Repeat(Time.time / CycleSeconds + StartDayT, 1f);
        float pitch = dayT * 360f;   // 0 sunrise · 90 noon · 180 sunset · 270 midnight
        _sun.transform.rotation = Quaternion.Euler(pitch, _baseYaw, 0f);

        float elevation = Mathf.Sin(pitch * Mathf.Deg2Rad);  // -1 (midnight) .. +1 (noon)
        float day       = Mathf.Clamp01(elevation);          // 0 all night, ramps up by day
        float horizon   = 1f - Mathf.Clamp01(Mathf.Abs(elevation) * 3f); // peaks at dawn/dusk

        // Base day<->night blend
        Color sunCol = Color.Lerp(NightSunColor, DaySunColor, day);
        float sunInt = Mathf.Lerp(NightSunIntensity, DaySunIntensity, day);
        Color amb    = Color.Lerp(NightAmbient, DayAmbient, day);

        // Warm tint at dawn/dusk (only while the sun is roughly up)
        sunCol = Color.Lerp(sunCol, DuskColor, horizon * Mathf.Clamp01(elevation + 0.25f));

        _sun.color     = sunCol;
        _sun.intensity = sunInt;
        RenderSettings.ambientLight = amb;
    }

    static Light FindSun()
    {
        if (RenderSettings.sun != null) return RenderSettings.sun;
        foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.type == LightType.Directional && l.enabled) return l;
        return null;
    }
}
