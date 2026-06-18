using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Singleton that plays a full-screen red flash whenever the wolf steals resources.
/// Self-initialising — calling StealEffect.Flash() from anywhere will auto-create
/// the Canvas overlay the first time. No scene setup required.
/// </summary>
public class StealEffect : MonoBehaviour
{
    // ── Singleton (lazy-create) ───────────────────────────────────────────────
    private static StealEffect _instance;
    public static StealEffect Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("StealEffect");
                _instance = go.AddComponent<StealEffect>();
            }
            return _instance;
        }
    }

    private Image _overlay;
    private Coroutine _active;

    // ── Constants ─────────────────────────────────────────────────────────────
    private const float DefaultIntensity = 0.32f;
    private const float FadeInTime       = 0.06f;
    private const float FadeOutTime      = 0.44f;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        BuildOverlay();
    }

    void BuildOverlay()
    {
        var canvasGO = new GameObject("StealEffectCanvas");
        canvasGO.transform.SetParent(transform, false);

        var canvas        = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200; // above everything, including lobby UI (100)
        canvasGO.AddComponent<CanvasScaler>();

        var imgGO = new GameObject("RedOverlay");
        imgGO.transform.SetParent(canvasGO.transform, false);
        _overlay              = imgGO.AddComponent<Image>();
        _overlay.color        = new Color(1f, 0f, 0f, 0f);
        _overlay.raycastTarget = false; // purely visual — never blocks input

        var rt = imgGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Flashes a red vignette on screen. Safe to call from any context.
    /// Interrupts any in-progress flash and starts fresh.
    /// </summary>
    public void Flash(float intensity = DefaultIntensity)
    {
        if (_overlay == null) return;
        if (_active != null) StopCoroutine(_active);
        _active = StartCoroutine(FlashRoutine(intensity));
    }

    // ── Private ───────────────────────────────────────────────────────────────

    IEnumerator FlashRoutine(float intensity)
    {
        // Fast in
        float t = 0;
        while (t < FadeInTime)
        {
            t += Time.deltaTime;
            _overlay.color = new Color(1f, 0f, 0f, Mathf.Lerp(0f, intensity, t / FadeInTime));
            yield return null;
        }

        // Slow out
        t = 0;
        while (t < FadeOutTime)
        {
            t += Time.deltaTime;
            _overlay.color = new Color(1f, 0f, 0f, Mathf.Lerp(intensity, 0f, t / FadeOutTime));
            yield return null;
        }

        _overlay.color = new Color(1f, 0f, 0f, 0f);
        _active = null;
    }
}
