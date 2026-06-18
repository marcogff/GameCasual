using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Self-spawning win condition + screen.
///
/// Watches every build site in the scene (MaterialsData where IsBuildSite is true).
/// When all of them are completed, shows a "You built the village!" overlay with
/// Play Again / Main Menu. Self-contained — no scene/prefab wiring.
///
/// If the scene has no build sites it stays dormant.
/// </summary>
public class WinScreen : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoSpawn()
    {
        if (FindFirstObjectByType<WinScreen>() != null) return;
        new GameObject("WinScreen").AddComponent<WinScreen>();
    }

    private MaterialsData[] _buildSites;
    private bool _won;
    private float _checkTimer;
    private const float CheckInterval = 0.5f;

    void Start() => StartCoroutine(FindBuildSitesNextFrame());

    IEnumerator FindBuildSitesNextFrame()
    {
        // Wait one frame so all MaterialsData have run Start()
        yield return null;
        var all = FindObjectsByType<MaterialsData>(FindObjectsSortMode.None);
        var list = new System.Collections.Generic.List<MaterialsData>();
        foreach (var m in all)
            if (m != null && m.IsBuildSite) list.Add(m);
        _buildSites = list.ToArray();
    }

    void Update()
    {
        if (_won || _buildSites == null || _buildSites.Length == 0) return;

        _checkTimer += Time.deltaTime;
        if (_checkTimer < CheckInterval) return;
        _checkTimer = 0f;

        foreach (var site in _buildSites)
            if (site == null || !site.IsCompleted) return; // not all done yet

        Win();
    }

    void Win()
    {
        _won = true;
        AudioManager.Instance.Play(AudioManager.BuildComplete);
        BuildUI();
    }

    void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ── UI ───────────────────────────────────────────────────────────────────────

    void BuildUI()
    {
        var canvasGO = new GameObject("WinCanvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas        = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 280;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.AddComponent<GraphicRaycaster>();

        var backdrop = MakeImage(canvasGO.transform, "Backdrop", new Color(0.03f, 0.10f, 0.05f, 0.92f));
        Stretch(backdrop);

        var panel = MakeImage(backdrop.transform, "Panel", new Color(0.10f, 0.16f, 0.12f, 1f)).gameObject;
        var pr = panel.GetComponent<RectTransform>();
        pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
        pr.sizeDelta = new Vector2(640f, 440f);

        var layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(44, 44, 44, 44);
        layout.spacing = 20f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        MakeLabel(panel.transform, "🏆", 64, Color.white);
        MakeLabel(panel.transform, "VILLAGE COMPLETE!", 44, new Color(0.5f, 1f, 0.65f), FontStyles.Bold);
        MakeLabel(panel.transform, "You gathered, built, and survived the wolf.", 20,
                  new Color(0.85f, 0.9f, 0.85f));
        MakeSpacer(panel.transform, 14f);
        MakeButton(panel.transform, "PLAY AGAIN", new Color(0.20f, 0.62f, 0.32f)).onClick.AddListener(Restart);
        MakeButton(panel.transform, "MAIN MENU",  new Color(0.30f, 0.42f, 0.70f)).onClick.AddListener(Restart);

        // Celebratory pop-in
        panel.transform.localScale = Vector3.zero;
        LeanTween.scale(panel, Vector3.one, 0.6f).setEaseOutBack();
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

    static Button MakeButton(Transform parent, string label, Color bg)
    {
        var go = new GameObject("Btn_" + label);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = bg;

        var btn = go.AddComponent<Button>();
        var cb = btn.colors;
        cb.highlightedColor = bg * 1.2f; cb.pressedColor = bg * 0.7f;
        btn.colors = cb;

        go.AddComponent<LayoutElement>().preferredHeight = 68f;

        var txtGO = new GameObject("Text");
        txtGO.transform.SetParent(go.transform, false);
        var txt = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text = label; txt.fontSize = 26f; txt.color = Color.white;
        txt.fontStyle = FontStyles.Bold; txt.alignment = TextAlignmentOptions.Center;
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
