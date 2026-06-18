using TMPro;
using UnityEngine;

/// <summary>
/// Floating "!" alert that appears above the wolf when it starts chasing the player.
/// Added programmatically to the Enemy GameObject in Enemy.Start() — no prefab edits needed.
///
/// Usage:
///   _alert = gameObject.AddComponent&lt;WolfAlert&gt;();
///   _alert.Show();   // call when entering Chase state
///   _alert.Hide();   // auto-called after 1.5 s; call manually when wolf backs off
/// </summary>
public class WolfAlert : MonoBehaviour
{
    private TextMeshPro _label;
    private Camera      _cam;

    private const float HeightAbovePivot = 2.3f;
    private const float WorldScale       = 0.014f;
    private const float AutoHideDelay    = 1.5f;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Start()
    {
        _cam = Camera.main;
        BuildLabel();
    }

    void BuildLabel()
    {
        var go = new GameObject("WolfAlert");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.up * HeightAbovePivot;
        go.transform.localScale    = Vector3.zero; // start hidden

        _label           = go.AddComponent<TextMeshPro>();
        _label.text      = "!";
        _label.fontSize  = 10f;
        _label.color     = new Color(1f, 0.88f, 0.1f); // warm yellow
        _label.fontStyle = FontStyles.Bold;
        _label.alignment = TextAlignmentOptions.Center;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Show()
    {
        if (_label == null) return;

        CancelInvoke(nameof(Hide));
        var go = _label.gameObject;
        go.SetActive(true);
        LeanTween.cancel(go);

        // Pop in with overshoot, then settle
        go.transform.localScale = Vector3.zero;
        LeanTween.scale(go, Vector3.one * WorldScale * 80f, 0.18f)
            .setEaseOutBack()
            .setOnComplete(() =>
            {
                // Subtle pulse while visible
                LeanTween.scale(go, Vector3.one * WorldScale * 70f, 0.3f)
                    .setLoopPingPong(-1); // stopped by LeanTween.cancel in Hide()
            });

        Invoke(nameof(Hide), AutoHideDelay);
    }

    public void Hide()
    {
        CancelInvoke(nameof(Hide));
        if (_label == null) return;

        var go = _label.gameObject;
        LeanTween.cancel(go);
        LeanTween.scale(go, Vector3.zero, 0.12f)
            .setEaseInBack()
            .setOnComplete(() => go.SetActive(false));
    }

    // ── Billboard ─────────────────────────────────────────────────────────────

    void LateUpdate()
    {
        if (_label == null || !_label.gameObject.activeSelf) return;
        if (_cam == null) { _cam = Camera.main; return; }

        _label.transform.LookAt(
            _label.transform.position + _cam.transform.rotation * Vector3.forward,
            _cam.transform.rotation   * Vector3.up);
    }
}
