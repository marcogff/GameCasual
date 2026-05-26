using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public Image upgradePanel;
    [SerializeField] private CanvasGroup _panelCanvas;
    [SerializeField] private CanvasGroup _woodCanvas;
    [SerializeField] private CanvasGroup _fishCanvas;

    // Assign a TMP_FontAsset in the inspector for the "gaming" font
    // Steps: import TTF → right-click → Create → TextMeshPro → Font Asset → drag here
    [SerializeField] private TMP_FontAsset _gameFont;

    private TextMeshProUGUI _woodText;
    private TextMeshProUGUI _fishText;
    private Button _upgradeButton;
    private Transform _upgradePanelInner;
    private CanvasGroup _upgradePanelInnerCanvas;
    private RectTransform _upgradePanelRect;

    private bool _prevHasMat;
    private bool _prevHasWood;
    private bool _prevHasFish;
    private bool _panelOpen;

    // Computed from actual canvas width in Start() so any screen size works
    private float _panelOffscreenX;
    private const int UpgradeCost = 20;

    // Grey tint applied to the button image when player can't afford the upgrade
    private static readonly Color AffordableColor  = Color.white;
    private static readonly Color UnaffordableColor = new Color(0.45f, 0.45f, 0.45f, 1f);

    void Start()
    {
        _woodText = _woodCanvas.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        _fishText = _fishCanvas.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        _upgradePanelInner       = upgradePanel.transform.GetChild(0);
        _upgradeButton           = _upgradePanelInner.GetChild(3).GetComponent<Button>();
        _upgradePanelInnerCanvas = _upgradePanelInner.GetComponent<CanvasGroup>();
        _upgradePanelRect        = upgradePanel.GetComponent<RectTransform>();

        // Ensure ColorTint transition is active so disabled state greys out properly
        _upgradeButton.transition = Selectable.Transition.ColorTint;
        ColorBlock cb = _upgradeButton.colors;
        cb.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.8f);
        _upgradeButton.colors = cb;

        // Panel starts off-screen and invisible — make sure it doesn't swallow clicks
        _upgradePanelInnerCanvas.blocksRaycasts = false;
        _upgradePanelInnerCanvas.interactable   = false;

        // Compute how far right the panel must travel to be fully off-screen on any device
        Canvas rootCanvas = GetComponentInParent<Canvas>();
        float canvasWidth = rootCanvas != null
            ? rootCanvas.GetComponent<RectTransform>().rect.width
            : 2000f;
        _panelOffscreenX = canvasWidth * 0.5f + _upgradePanelRect.rect.width;

        // Dark background + outline on each resource counter so they read against any colour
        SetupResourceCounter(_woodCanvas);
        SetupResourceCounter(_fishCanvas);

        // Text outline on the count labels
        ApplyTextOutline(_woodText);
        ApplyTextOutline(_fishText);

        if (_gameFont != null)
            ApplyFontToAll();
    }

    void Update()
    {
        UpdateResourceDisplay();
        UpdateUpgradeButton();
    }

    private void UpdateResourceDisplay()
    {
        var player = GameManager.Instance.playerController;
        bool hasMat  = player.hasMat;
        bool hasWood = player.currentElementsWood.Count > 0;
        bool hasFish = player.currentElementsFish.Count > 0;

        if (hasMat != _prevHasMat)
        {
            LeanTween.alphaCanvas(_panelCanvas, hasMat ? 1f : 0f, .2f);
            _prevHasMat = hasMat;
        }

        if (hasWood != _prevHasWood)
        {
            _woodCanvas.gameObject.SetActive(hasWood);
            if (hasWood) LeanTween.alphaCanvas(_woodCanvas, 1f, .2f);
            _prevHasWood = hasWood;
        }
        if (hasWood)
            _woodText.text = player.currentElementsWood.Count.ToString();

        if (hasFish != _prevHasFish)
        {
            _fishCanvas.gameObject.SetActive(hasFish);
            if (hasFish) LeanTween.alphaCanvas(_fishCanvas, 1f, .2f);
            _prevHasFish = hasFish;
        }
        if (hasFish)
            _fishText.text = player.currentElementsFish.Count.ToString();
    }

    private void UpdateUpgradeButton()
    {
        bool canAfford = GameManager.Instance.playerController.currentElementsFish.Count >= UpgradeCost;
        _upgradeButton.interactable = canAfford;

        // Extra visual: tint the button image directly so the grey is obvious on mobile
        var btnImage = _upgradeButton.GetComponent<Image>();
        if (btnImage != null)
            btnImage.color = canAfford ? AffordableColor : UnaffordableColor;
    }

    public void MoreSpeed()
    {
        GameManager.Instance.playerController.speedUpgrade = true;
        GameManager.Instance.playerController.currentElementsFish.RemoveRange(0, UpgradeCost);

        _upgradePanelInner.GetComponent<Image>().enabled = true;
        _upgradeButton.GetComponent<Image>().enabled = false;
        _upgradeButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "EQUIPPED";
    }

    public void ShowUpgradePanel(bool show)
    {
        if (show && _panelOpen) return; // don't re-open while already open
        _panelOpen = show;

        LeanTween.cancel(_upgradePanelRect.gameObject);

        if (show)
        {
            _upgradePanelInnerCanvas.blocksRaycasts = true;
            _upgradePanelInnerCanvas.interactable   = true;
            LeanTween.moveX(_upgradePanelRect, 0, .35f).setEaseOutBack().setOnComplete(FadeInElements);
        }
        else
        {
            LeanTween.cancel(_upgradeButton.gameObject);
            _upgradeButton.transform.localScale = Vector3.one;
            LeanTween.moveX(_upgradePanelRect, _panelOffscreenX, .35f).setEaseOutBack().setOnComplete(FadeOutElements);
        }
    }

    // Hard close — always works regardless of _panelOpen state.
    // Assign this to the close/back button's OnClick in the inspector.
    public void CloseUpgradePanel()
    {
        _panelOpen = false;
        LeanTween.cancel(_upgradePanelRect.gameObject);
        LeanTween.cancel(_upgradeButton.gameObject);
        _upgradeButton.transform.localScale = Vector3.one;
        LeanTween.moveX(_upgradePanelRect, _panelOffscreenX, .35f).setEaseOutBack().setOnComplete(FadeOutElements);
    }

    private void FadeInElements()
    {
        LeanTween.alphaCanvas(_upgradePanelInnerCanvas, 1, .4f)
            .setOnComplete(() => LeanTween.scale(_upgradeButton.gameObject, new Vector3(.9f, .9f, .9f), .4f).setLoopPingPong());
    }

    private void FadeOutElements()
    {
        _upgradePanelInnerCanvas.blocksRaycasts = false;
        _upgradePanelInnerCanvas.interactable   = false;
        LeanTween.alphaCanvas(_upgradePanelInnerCanvas, 0, .3f);
    }

    // Applies _gameFont to all TMP texts owned by the HUD
    private void ApplyFontToAll()
    {
        foreach (var tmp in GetComponentsInChildren<TextMeshProUGUI>(includeInactive: true))
            tmp.font = _gameFont;
    }

    // Dark semi-transparent background + Unity UI Outline on every Image inside the
    // resource counter panel so the whole widget pops against any background colour.
    private static void SetupResourceCounter(CanvasGroup canvas)
    {
        if (canvas == null) return;

        // Darken the root background image if one exists
        var bg = canvas.GetComponent<Image>();
        if (bg != null)
            bg.color = new Color(0f, 0f, 0f, 0.55f);

        // Add a pixel-perfect Outline to every Image in the hierarchy
        foreach (var img in canvas.GetComponentsInChildren<Image>(includeInactive: true))
        {
            if (img.GetComponent<Outline>() != null) continue;
            var outline = img.gameObject.AddComponent<Outline>();
            outline.effectColor    = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(2f, -2f);
        }
    }

    // Black outline + drop shadow on TMP text so count numbers read against any background.
    // Uses fontMaterial to create a per-instance material — other texts are unaffected.
    private static void ApplyTextOutline(TextMeshProUGUI text)
    {
        if (text == null) return;
        text.color = Color.white;

        var mat = text.fontMaterial;
        mat.EnableKeyword("OUTLINE_ON");
        mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.25f);
        mat.SetColor(ShaderUtilities.ID_OutlineColor, Color.black);

        mat.EnableKeyword("UNDERLAY_ON");
        mat.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0.8f));
        mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX,  0.6f);
        mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.6f);
        mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.05f);
    }
}
