using System.Text;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Phase 5 — In-session multiplayer HUD.
///
/// Self-contained: auto-spawns after the scene loads and builds its own Canvas,
/// so there is nothing to wire up in the Inspector (same pattern as StealEffect
/// and LobbyUI).
///
/// Shows, only while a networked session is running:
///   • Join code (host only) — top-right, so the host can read it to a friend
///   • Teammate resource counts — top-left, one row per OTHER player
///   • "Waiting for a friend to join…" until a second player is connected
///
/// In solo play (no NetworkManager listening) the whole HUD stays hidden.
/// </summary>
public class MultiplayerHUD : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoSpawn()
    {
        if (FindFirstObjectByType<MultiplayerHUD>() != null) return;
        var go = new GameObject("MultiplayerHUD");
        go.AddComponent<MultiplayerHUD>();
    }

    // ── UI refs ───────────────────────────────────────────────────────────────
    private GameObject      _root;
    private TextMeshProUGUI _joinCodeText;
    private TextMeshProUGUI _teammateText;
    private TextMeshProUGUI _waitingText;

    // ── State ───────────────────────────────────────────────────────────────
    private string _joinCode;
    private bool   _subscribed;
    private float  _refreshTimer;

    private const float RefreshInterval = 0.4f;   // how often to re-read teammate counts
    private readonly StringBuilder _sb = new StringBuilder(128);

    // ── Lifecycle ───────────────────────────────────────────────────────────

    void Start()
    {
        BuildUI();
        _root.SetActive(false);
    }

    void Update()
    {
        TrySubscribe();

        bool inSession = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        if (_root.activeSelf != inSession) _root.SetActive(inSession);
        if (!inSession) return;

        _refreshTimer += Time.deltaTime;
        if (_refreshTimer < RefreshInterval) return;
        _refreshTimer = 0f;

        RefreshTeammates();
        RefreshWaiting();
    }

    void OnDestroy()
    {
        if (_subscribed && LobbyManager.Instance != null)
            LobbyManager.Instance.OnJoinCodeReady -= HandleJoinCode;
    }

    // ── Lobby hook ─────────────────────────────────────────────────────────────

    void TrySubscribe()
    {
        if (_subscribed || LobbyManager.Instance == null) return;
        LobbyManager.Instance.OnJoinCodeReady += HandleJoinCode;
        _subscribed = true;
    }

    void HandleJoinCode(string code)
    {
        _joinCode = code;
        if (_joinCodeText != null)
        {
            _joinCodeText.text = $"JOIN CODE\n<size=130%><b>{code}</b></size>";
            _joinCodeText.gameObject.SetActive(true);
        }
    }

    // ── Per-refresh updates ────────────────────────────────────────────────────

    void RefreshTeammates()
    {
        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        _sb.Clear();
        int teammates = 0;
        foreach (var pc in players)
        {
            if (pc == null || pc.IsOwner || !pc.IsSpawned) continue; // skip myself / unspawned
            teammates++;
            _sb.Append($"Player {pc.OwnerClientId + 1}   ")
               .Append($"Wood {pc.NetWoodCount.Value}   ")
               .Append($"Fish {pc.NetFishCount.Value}\n");
        }

        _teammateText.text = _sb.ToString();
        _teammateText.gameObject.SetActive(teammates > 0);
    }

    void RefreshWaiting()
    {
        // "Waiting" only matters for the host before anyone joins
        bool isHost      = NetworkManager.Singleton.IsHost;
        int  connected   = NetworkManager.Singleton.ConnectedClientsIds.Count;
        bool waiting     = isHost && connected < 2 && !string.IsNullOrEmpty(_joinCode);

        if (_waitingText.gameObject.activeSelf != waiting)
            _waitingText.gameObject.SetActive(waiting);
    }

    // ── UI construction ─────────────────────────────────────────────────────────

    void BuildUI()
    {
        _root = new GameObject("MultiplayerHUDCanvas");
        _root.transform.SetParent(transform, false);

        var canvas        = _root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90; // under StealEffect(200)/Lobby(100), above gameplay HUD
        _root.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        _root.AddComponent<GraphicRaycaster>();

        // Join code — top-right
        _joinCodeText = MakeText("JoinCode", new Vector2(1f, 1f), new Vector2(-24f, -24f),
                                 TextAlignmentOptions.TopRight, 26, new Color(0.4f, 1f, 0.55f));
        _joinCodeText.gameObject.SetActive(false);

        // Teammate counts — top-left
        _teammateText = MakeText("Teammates", new Vector2(0f, 1f), new Vector2(24f, -24f),
                                 TextAlignmentOptions.TopLeft, 24, Color.white);
        _teammateText.gameObject.SetActive(false);

        // Waiting banner — top-center
        _waitingText = MakeText("Waiting", new Vector2(0.5f, 1f), new Vector2(0f, -90f),
                                TextAlignmentOptions.Top, 26, new Color(1f, 0.9f, 0.4f));
        _waitingText.text = "Waiting for a friend to join…";
        _waitingText.gameObject.SetActive(false);
    }

    TextMeshProUGUI MakeText(string name, Vector2 anchor, Vector2 anchoredPos,
                             TextAlignmentOptions align, int size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(_root.transform, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot     = anchor;
        rt.sizeDelta = new Vector2(520f, 200f);
        rt.anchoredPosition = anchoredPos;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize  = size;
        tmp.color     = color;
        tmp.alignment = align;
        tmp.fontStyle = FontStyles.Bold;

        // Dark outline so it reads against any background
        var mat = tmp.fontMaterial;
        mat.EnableKeyword("OUTLINE_ON");
        mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.2f);
        mat.SetColor(ShaderUtilities.ID_OutlineColor, Color.black);

        return tmp;
    }
}
