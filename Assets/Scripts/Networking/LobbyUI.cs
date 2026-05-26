using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Phase 1 — Lobby overlay UI.
/// Creates its own Canvas programmatically so no prefab setup is needed.
/// Visible before a session starts; hides once connected.
///
/// Layout:
///   ┌─────────────────────────────┐
///   │     🐺  Play Together        │
///   │  [  HOST  ]  [  JOIN  ]     │
///   │         ──── or ────        │
///   │  Code: [  ______  ] [JOIN]  │
///   │  Status: ...                │
///   │  Join Code: ABCD12  (host)  │
///   └─────────────────────────────┘
/// </summary>
public class LobbyUI : MonoBehaviour
{
    // ── References built in BuildUI() ─────────────────────────────────────
    private GameObject     _panel;
    private TMP_InputField _codeInput;
    private TextMeshProUGUI _codeDisplay;
    private TextMeshProUGUI _statusText;
    private Button          _hostBtn;
    private Button          _joinBtn;

    private const string DefaultStatus = "Host a session or join a friend's code.";

    // ── Lifecycle ─────────────────────────────────────────────────────────

    void Start()
    {
        BuildUI();
        SubscribeToLobby();
    }

    void SubscribeToLobby()
    {
        if (LobbyManager.Instance == null) return;
        LobbyManager.Instance.OnJoinCodeReady  += code  => ShowCode(code);
        LobbyManager.Instance.OnSessionStarted += ()    => HidePanel();
        LobbyManager.Instance.OnError          += msg   => SetStatus(msg, Color.red);
    }

    // ── Panel visibility ─────────────────────────────────────────────────

    void HidePanel() => _panel.SetActive(false);
    void ShowPanel() => _panel.SetActive(true);

    void ShowCode(string code)
    {
        _codeDisplay.text = $"Your code: <b>{code}</b>\nShare it with your friend!";
        _codeDisplay.gameObject.SetActive(true);
        SetStatus("Waiting for players…", Color.yellow);
    }

    void SetStatus(string msg, Color color)
    {
        _statusText.text  = msg;
        _statusText.color = color;
    }

    // ── Button handlers ───────────────────────────────────────────────────

    void OnHostClicked()
    {
        _hostBtn.interactable = false;
        _joinBtn.interactable = false;
        SetStatus("Creating session…", Color.white);
        LobbyManager.Instance?.CreateLobby();
    }

    void OnJoinClicked()
    {
        string code = _codeInput.text.Trim();
        if (string.IsNullOrEmpty(code)) { SetStatus("Enter a join code first.", Color.red); return; }
        _hostBtn.interactable = false;
        _joinBtn.interactable = false;
        SetStatus("Joining…", Color.white);
        LobbyManager.Instance?.JoinLobby(code);
    }

    // ── Programmatic UI build ─────────────────────────────────────────────

    void BuildUI()
    {
        // Root canvas
        var canvasGO = new GameObject("LobbyCanvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode      = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder    = 100;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Semi-transparent dark backdrop
        var backdrop = MakeImage(canvasGO.transform, "Backdrop", new Color(0f, 0f, 0f, 0.85f));
        Stretch(backdrop);

        // Centered panel
        _panel = MakeImage(backdrop.transform, "Panel", new Color(0.1f, 0.1f, 0.15f, 1f)).gameObject;
        var panelRect = _panel.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(520f, 380f);
        panelRect.anchoredPosition = Vector2.zero;

        // Layout group inside panel
        var layout = _panel.AddComponent<VerticalLayoutGroup>();
        layout.padding      = new RectOffset(30, 30, 30, 30);
        layout.spacing      = 18f;
        layout.childForceExpandWidth  = true;
        layout.childForceExpandHeight = false;
        layout.childAlignment         = TextAnchor.UpperCenter;

        // Title
        MakeLabel(_panel.transform, "🐺  Play Together", 28, Color.white, FontStyles.Bold);

        // Subtitle
        MakeLabel(_panel.transform, "Collect resources together and build faster!", 16,
                  new Color(0.8f, 0.8f, 0.8f));

        // Divider
        MakeSpacer(_panel.transform, 4f);

        // HOST button
        _hostBtn = MakeButton(_panel.transform, "HOST A SESSION", new Color(0.2f, 0.6f, 0.3f));
        _hostBtn.onClick.AddListener(OnHostClicked);

        // Or label
        MakeLabel(_panel.transform, "─── or ───", 14, new Color(0.6f, 0.6f, 0.6f));

        // Code input row
        var row = MakeRow(_panel.transform, 54f);
        _codeInput = MakeInput(row.transform, "Enter join code…");
        _joinBtn   = MakeButton(row.transform, "JOIN", new Color(0.2f, 0.4f, 0.7f), 140f);
        _joinBtn.onClick.AddListener(OnJoinClicked);

        // Status text
        _statusText = MakeLabel(_panel.transform, DefaultStatus, 15, new Color(0.85f, 0.85f, 0.85f));

        // Code display (hidden until hosting)
        _codeDisplay = MakeLabel(_panel.transform, "", 17, new Color(0.3f, 0.9f, 0.5f));
        _codeDisplay.gameObject.SetActive(false);
    }

    // ── UI helpers ────────────────────────────────────────────────────────

    static Image MakeImage(Transform parent, string name, Color color)
    {
        var go   = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img  = go.AddComponent<Image>();
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
        tmp.text       = text;
        tmp.fontSize   = size;
        tmp.color      = color;
        tmp.fontStyle  = style;
        tmp.alignment  = TextAlignmentOptions.Center;
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = size * 1.6f;
        return tmp;
    }

    static Button MakeButton(Transform parent, string label, Color bg, float width = -1f)
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
        le.preferredHeight = 54f;
        if (width > 0f) le.preferredWidth = width;

        var txtGO = new GameObject("Text");
        txtGO.transform.SetParent(go.transform, false);
        var txt = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text      = label;
        txt.fontSize  = 18f;
        txt.color     = Color.white;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;
        Stretch(txt);

        return btn;
    }

    static TMP_InputField MakeInput(Transform parent, string placeholder)
    {
        var go  = new GameObject("Input");
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.08f, 0.08f, 0.12f, 1f);

        var field = go.AddComponent<TMP_InputField>();
        field.characterLimit = 8;

        // Text area child
        var textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        var text = textGO.AddComponent<TextMeshProUGUI>();
        text.fontSize = 18f;
        text.color    = Color.white;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        Stretch(text);
        text.margin = new Vector4(8, 0, 8, 0);

        // Placeholder child
        var phGO = new GameObject("Placeholder");
        phGO.transform.SetParent(go.transform, false);
        var ph = phGO.AddComponent<TextMeshProUGUI>();
        ph.text      = placeholder;
        ph.fontSize  = 16f;
        ph.color     = new Color(0.5f, 0.5f, 0.5f);
        ph.fontStyle = FontStyles.Italic;
        ph.alignment = TextAlignmentOptions.MidlineLeft;
        Stretch(ph);
        ph.margin = new Vector4(8, 0, 8, 0);

        field.textViewport   = go.GetComponent<RectTransform>();
        field.textComponent  = text;
        field.placeholder    = ph;

        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 54f;
        le.flexibleWidth   = 1f;

        return field;
    }

    static GameObject MakeRow(Transform parent, float height)
    {
        var go  = new GameObject("Row");
        go.transform.SetParent(parent, false);
        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10f;
        hlg.childForceExpandHeight = true;
        hlg.childForceExpandWidth  = false;
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.flexibleWidth   = 1f;
        return go;
    }

    static void MakeSpacer(Transform parent, float height)
    {
        var go = new GameObject("Spacer");
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = height;
    }
}
