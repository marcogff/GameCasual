using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Self-spawning pause system: a small "II" button (top-right) that freezes the
/// game and shows Resume / Restart / Main Menu. No scene/prefab wiring.
/// All animations/handlers use unscaled time so they work while paused.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoSpawn()
    {
        if (FindFirstObjectByType<PauseMenu>() != null) return;
        new GameObject("PauseMenu").AddComponent<PauseMenu>();
    }

    private GameObject _panel;
    private bool _paused;

    void Start() => BuildUI();

    // ── Actions ─────────────────────────────────────────────────────────────────

    void TogglePause()
    {
        AudioManager.Instance.Play(AudioManager.UiClick);
        _paused = !_paused;
        _panel.SetActive(_paused);
        Time.timeScale = _paused ? 0f : 1f;
        if (_paused)
        {
            _panel.transform.localScale = Vector3.zero;
            LeanTween.scale(_panel, Vector3.one, 0.25f).setEaseOutBack().setIgnoreTimeScale(true);
        }
    }

    void Resume()
    {
        AudioManager.Instance.Play(AudioManager.UiClick);
        _paused = false;
        _panel.SetActive(false);
        Time.timeScale = 1f;
    }

    void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ── UI ───────────────────────────────────────────────────────────────────────

    void BuildUI()
    {
        var canvasGO = new GameObject("PauseCanvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas        = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 250; // above HUD, below main menu (300)
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Pause button — top-right
        var btn = MakeButton(canvasGO.transform, "II", new Color(0f, 0f, 0f, 0.45f), 96f);
        var brt = btn.GetComponent<RectTransform>();
        brt.anchorMin = brt.anchorMax = brt.pivot = new Vector2(1f, 1f);
        brt.anchoredPosition = new Vector2(-24f, -24f);
        btn.onClick.AddListener(TogglePause);

        // Pause panel (hidden until paused)
        var backdrop = MakeImage(canvasGO.transform, "Backdrop", new Color(0f, 0f, 0f, 0.7f));
        Stretch(backdrop);
        _panel = backdrop.gameObject;

        var inner = MakeImage(backdrop.transform, "Panel", new Color(0.10f, 0.13f, 0.17f, 1f)).gameObject;
        var pr = inner.GetComponent<RectTransform>();
        pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
        pr.sizeDelta = new Vector2(520f, 420f);

        var layout = inner.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(40, 40, 40, 40);
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        MakeLabel(inner.transform, "PAUSED", 40, Color.white, FontStyles.Bold);
        MakeSpacer(inner.transform, 10f);
        MakeButton(inner.transform, "RESUME",    new Color(0.20f, 0.62f, 0.32f)).onClick.AddListener(Resume);
        MakeButton(inner.transform, "RESTART",   new Color(0.30f, 0.42f, 0.70f)).onClick.AddListener(Restart);
        MakeButton(inner.transform, "MAIN MENU", new Color(0.45f, 0.20f, 0.22f)).onClick.AddListener(Restart);

        _panel.SetActive(false);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────
    static Image MakeImage(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    static void Stretch(Component c)
    {
        var r = c.GetComponent<RectTransform>();
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }

    static TextMeshProUGUI MakeLabel(Transform parent, string text, int size, Color color,
                                     FontStyles style = FontStyles.Normal)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.color = color;
        tmp.fontStyle = style; tmp.alignment = TextAlignmentOptions.Center;
        go.AddComponent<LayoutElement>().preferredHeight = size * 1.5f;
        return tmp;
    }

    static Button MakeButton(Transform parent, string label, Color bg, float square = -1f)
    {
        var go = new GameObject("Btn_" + label);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = bg;

        var btn = go.AddComponent<Button>();
        var cb = btn.colors;
        cb.highlightedColor = bg * 1.2f; cb.pressedColor = bg * 0.7f;
        btn.colors = cb;

        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = square > 0 ? square : 66f;
        if (square > 0) le.preferredWidth = square;

        var txtGO = new GameObject("Text");
        txtGO.transform.SetParent(go.transform, false);
        var txt = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text = label; txt.fontSize = square > 0 ? 40f : 24f;
        txt.color = Color.white; txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;
        Stretch(txt);
        return btn;
    }

    static void MakeSpacer(Transform parent, float height)
    {
        var go = new GameObject("Spacer");
        go.transform.SetParent(parent, false);
        go.AddComponent<LayoutElement>().preferredHeight = height;
    }
}
