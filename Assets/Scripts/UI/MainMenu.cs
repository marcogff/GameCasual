using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Self-spawning main menu shown at launch.
///
/// • Auto-creates after the scene loads (no prefab/scene wiring — same pattern as
///   LobbyUI / MultiplayerHUD / StealEffect).
/// • Freezes the world behind it (Time.timeScale = 0) until PLAY is pressed.
/// • All animations use unscaled time so they run while the game is paused.
///
/// To change the game name / tagline, edit GameTitle / Tagline below.
/// </summary>
public class MainMenu : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoSpawn()
    {
        if (FindFirstObjectByType<MainMenu>() != null) return;
        new GameObject("MainMenu").AddComponent<MainMenu>();
    }

    private const string GameTitle = "WOODLAND RUSH";
    private const string Tagline   = "Gather wood & fish  •  Build your land  •  Outwit the wolf";

    private GameObject _root;
    private GameObject _panel;
    private Image      _fade;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Start()
    {
        BuildUI();
        Time.timeScale = 0f;            // freeze gameplay behind the menu
        StartCoroutine(IntroAnim());
    }

    IEnumerator IntroAnim()
    {
        // Panel pops in (unscaled — we're paused)
        _panel.transform.localScale = Vector3.zero;
        LeanTween.scale(_panel, Vector3.one, 0.5f).setEaseOutBack().setIgnoreTimeScale(true);
        yield return null;
    }

    // ── Button handlers ─────────────────────────────────────────────────────────

    void OnPlay()
    {
        // Slide/scale the menu away while still paused, then resume.
        LeanTween.scale(_panel, Vector3.zero, 0.3f).setEaseInBack().setIgnoreTimeScale(true);
        LeanTween.alpha(_fade.rectTransform, 0f, 0.3f).setIgnoreTimeScale(true)
            .setOnComplete(() =>
            {
                Time.timeScale = 1f;
                _root.SetActive(false);
            });
    }

    void OnQuit()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ── UI construction ─────────────────────────────────────────────────────────

    void BuildUI()
    {
        _root = new GameObject("MainMenuCanvas");
        _root.transform.SetParent(transform, false);

        var canvas        = _root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300; // above everything
        _root.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        _root.AddComponent<GraphicRaycaster>();

        // Full-screen dark backdrop (doubles as the fade overlay)
        _fade = MakeImage(_root.transform, "Backdrop", new Color(0.04f, 0.06f, 0.08f, 0.96f));
        Stretch(_fade);

        // Centered panel
        _panel = MakeImage(_fade.transform, "Panel", new Color(0.10f, 0.13f, 0.17f, 1f)).gameObject;
        var pr = _panel.GetComponent<RectTransform>();
        pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
        pr.sizeDelta = new Vector2(620f, 460f);
        pr.anchoredPosition = Vector2.zero;

        var layout = _panel.AddComponent<VerticalLayoutGroup>();
        layout.padding        = new RectOffset(40, 40, 44, 40);
        layout.spacing        = 20f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childForceExpandWidth  = true;
        layout.childForceExpandHeight = false;

        // Title + tagline
        MakeLabel(_panel.transform, GameTitle, 50, new Color(0.45f, 1f, 0.6f), FontStyles.Bold);
        MakeLabel(_panel.transform, Tagline,  20, new Color(0.8f, 0.85f, 0.85f));
        MakeSpacer(_panel.transform, 16f);

        // Buttons
        MakeButton(_panel.transform, "PLAY", new Color(0.20f, 0.62f, 0.32f)).onClick.AddListener(OnPlay);
        MakeButton(_panel.transform, "QUIT", new Color(0.45f, 0.20f, 0.22f)).onClick.AddListener(OnQuit);

        MakeSpacer(_panel.transform, 6f);
        MakeLabel(_panel.transform, "v0.1  •  prototype", 14, new Color(0.5f, 0.5f, 0.55f));
    }

    // ── UI helpers (mirror LobbyUI's style) ──────────────────────────────────────

    static Image MakeImage(Transform parent, string name, Color color)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    static void Stretch(Component c)
    {
        var r = c.GetComponent<RectTransform>();
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }

    static TextMeshProUGUI MakeLabel(Transform parent, string text, int size,
                                     Color color, FontStyles style = FontStyles.Normal)
    {
        var go  = new GameObject("Label");
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.color     = color;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = size * 1.5f;
        return tmp;
    }

    static Button MakeButton(Transform parent, string label, Color bg)
    {
        var go  = new GameObject("Btn_" + label);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = bg;

        var btn = go.AddComponent<Button>();
        var cb  = btn.colors;
        cb.highlightedColor = bg * 1.2f;
        cb.pressedColor     = bg * 0.7f;
        btn.colors = cb;

        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 70f;

        var txtGO = new GameObject("Text");
        txtGO.transform.SetParent(go.transform, false);
        var txt = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text      = label;
        txt.fontSize  = 26f;
        txt.color     = Color.white;
        txt.fontStyle = FontStyles.Bold;
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
