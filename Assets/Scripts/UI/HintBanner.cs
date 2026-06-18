using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Self-spawning onboarding hints. Shows a few tips at the bottom of the screen at
/// the start of a run, one after another, then hides itself.
///
/// Timing uses scaled time, so the hints wait behind the main menu (Time.timeScale
/// = 0) and only start cycling once the player presses PLAY.
/// Self-contained — no scene/prefab wiring.
/// </summary>
public class HintBanner : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoSpawn()
    {
        if (FindFirstObjectByType<HintBanner>() != null) return;
        new GameObject("HintBanner").AddComponent<HintBanner>();
    }

    private static readonly string[] Hints =
    {
        "Drag anywhere to move",
        "Gather wood & fish, then carry them to a build site",
        "Deposit resources to unlock new land",
        "Watch out — the wolf steals what you're carrying!",
    };

    private const float ShowTime = 3.4f;   // seconds each hint stays up
    private const float FadeTime = 0.4f;

    private CanvasGroup _group;
    private TextMeshProUGUI _text;

    void Start()
    {
        BuildUI();
        StartCoroutine(CycleHints());
    }

    IEnumerator CycleHints()
    {
        // Small delay so it doesn't appear the very instant the menu closes
        yield return new WaitForSeconds(0.6f);

        foreach (var hint in Hints)
        {
            _text.text = hint;
            yield return Fade(0f, 1f);
            yield return new WaitForSeconds(ShowTime);
            yield return Fade(1f, 0f);
            yield return new WaitForSeconds(0.25f);
        }

        Destroy(gameObject);
    }

    IEnumerator Fade(float from, float to)
    {
        float t = 0f;
        while (t < FadeTime)
        {
            t += Time.deltaTime;
            _group.alpha = Mathf.Lerp(from, to, t / FadeTime);
            yield return null;
        }
        _group.alpha = to;
    }

    void BuildUI()
    {
        var canvasGO = new GameObject("HintCanvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas        = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 80;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        _group = canvasGO.AddComponent<CanvasGroup>();
        _group.alpha = 0f;
        _group.blocksRaycasts = false; // never eat input

        // Dark pill near the bottom
        var pill = new GameObject("Pill", typeof(RectTransform));
        pill.transform.SetParent(canvasGO.transform, false);
        var prt = pill.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.5f, 0f);
        prt.anchorMax = new Vector2(0.5f, 0f);
        prt.pivot     = new Vector2(0.5f, 0f);
        prt.anchoredPosition = new Vector2(0f, 140f);
        prt.sizeDelta = new Vector2(820f, 76f);
        var img = pill.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.6f);

        _text = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        _text.transform.SetParent(pill.transform, false);
        var trt = _text.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(20f, 0f); trt.offsetMax = new Vector2(-20f, 0f);
        _text.fontSize  = 26f;
        _text.color     = Color.white;
        _text.fontStyle = FontStyles.Bold;
        _text.alignment = TextAlignmentOptions.Center;
    }
}
