using TMPro;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Phase 2 — Floating billboard name tag above networked players.
/// Add this component to the player prefab alongside NetworkObject.
/// Automatically hides on the local (owning) player and shows on remote players.
/// </summary>
public class PlayerNameTag : NetworkBehaviour
{
    [SerializeField] private float _height    = 2.4f;   // world-units above pivot
    [SerializeField] private float _textScale = 0.012f; // world scale for 3-D text

    private TextMeshPro _label;
    private Camera      _cam;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public override void OnNetworkSpawn()
    {
        BuildLabel();
        _cam = Camera.main;
    }

    void LateUpdate()
    {
        if (_label == null || _cam == null) return;

        // Billboard — keep the label facing the camera every frame
        _label.transform.LookAt(
            _label.transform.position + _cam.transform.rotation * Vector3.forward,
            _cam.transform.rotation   * Vector3.up);
    }

    // ── Build ─────────────────────────────────────────────────────────────────

    void BuildLabel()
    {
        var go = new GameObject("NameTag");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.up * _height;
        go.transform.localScale    = Vector3.one * _textScale;

        _label           = go.AddComponent<TextMeshPro>();
        _label.text      = $"Player {OwnerClientId + 1}";
        _label.fontSize  = 6f;
        _label.color     = IsOwner ? new Color(0.4f, 1f, 0.5f) : Color.white;
        _label.fontStyle = FontStyles.Bold;
        _label.alignment = TextAlignmentOptions.Center;

        // Local player doesn't need to see their own tag
        go.SetActive(!IsOwner);
    }
}
