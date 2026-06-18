using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Singleton that plays a full-screen colour flash.
/// Self-initialising — calling StealEffect.Flash() from anywhere will auto-create
/// the Canvas overlay the first time. No scene setup required.
///
/// Usage:
///   StealEffect.Instance.Flash();                    // red flash (wolf steal)
///   StealEffect.Instance.Flash(Color.green, 0.45f);  // green flash (build complete)
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

    // ── Default values ────────────────────────────────────────────────────────
    private static readonly Color StealColor     = new Color(1f, 0f, 0f);
    private static readonly Color CelebColor     = new Color(0.35f, 1f, 0.4f);
    private const float DefaultStealIntensity    = 0.32f;
    private const float DefaultCelebIntensity    = 0.25f;
    private const float FadeInTime               = 0.06f;
    private const float StealFadeOutTime         = 0.44f;
    private const float CelebFadeOutTime         = 0.70f;

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
        canvas.sortingOrder = 200;
        canvasGO.AddComponent<CanvasScaler>();

        var imgGO = new GameObject("Overlay");
        imgGO.transform.SetParent(canvasGO.transform, false);
        _overlay              = imgGO.AddComponent<Image>();
        _overlay.color        = new Color(1f, 0f, 0f, 0f);
        _overlay.raycastTarget = false;

        var rt = imgGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>Red flash — wolf stole resources.</summary>
    public void Flash(float intensity = DefaultStealIntensity)
        => Trigger(StealColor, intensity, StealFadeOutTime);

    /// <summary>Green flash — build site completed.</summary>
    public void Celebrate(float intensity = DefaultCelebIntensity)
        => Trigger(CelebColor, intensity, CelebFadeOutTime);

    /// <summary>Flash any colour.</summary>
    public void Flash(Color color, float intensity, float fadeOut = StealFadeOutTime)
        => Trigger(color, intensity, fadeOut);

    // ── Private ───────────────────────────────────────────────────────────────

    void Trigger(Color color, float intensity, float fadeOut)
    {
        if (_overlay == null) return;
        if (_active != null) StopCoroutine(_active);
        _active = StartCoroutine(FlashRoutine(color, intensity, fadeOut));
    }

    IEnumerator FlashRoutine(Color color, float intensity, float fadeOut)
    {
        float t = 0;
        while (t < FadeInTime)
        {
            t += Time.deltaTime;
            _overlay.color = new Color(color.r, color.g, color.b,
                                       Mathf.Lerp(0f, intensity, t / FadeInTime));
            yield return null;
        }

        t = 0;
        while (t < fadeOut)
        {
            t += Time.deltaTime;
            _overlay.color = new Color(color.r, color.g, color.b,
                                       Mathf.Lerp(intensity, 0f, t / fadeOut));
            yield return null;
        }

        _overlay.color = new Color(color.r, color.g, color.b, 0f);
        _active = null;
    }
}
