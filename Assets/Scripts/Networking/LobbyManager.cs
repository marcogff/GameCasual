using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;   // RelayServerData
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;              // NOTE: package id is "com.unity.services.lobby" (singular),
using Unity.Services.Lobbies.Models;       //       but the C# namespace is "Lobbies" (plural)
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

/// <summary>
/// Phase 1 — Foundation.
/// Handles Unity Gaming Services init, Relay allocation, and Lobby creation/join.
/// Works alongside NetworkManager (added to scene via Tools → Setup Multiplayer).
///
/// Flow:
///   Host: CreateLobby()  →  gets join code  →  shares with friend
///   Client: JoinLobby(code)  →  game starts
/// </summary>
public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    // ── Events ─────────────────────────────────────────────────────────────
    public event Action<string> OnJoinCodeReady;   // UI shows this code to host
    public event Action         OnSessionStarted;  // hide lobby UI, start game
    public event Action<string> OnError;           // show error message

    // ── State ───────────────────────────────────────────────────────────────
    private Lobby  _currentLobby;
    private bool   _ugsReady;
    private float  _heartbeatTimer;

    private const int   MaxPlayers        = 4;
    private const float HeartbeatInterval = 15f;   // Unity deletes lobby after 30s silence

    // ── Lifecycle ────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    async void Start()
    {
        await InitUGS();
    }

    async Task InitUGS()
    {
        try
        {
            await UnityServices.InitializeAsync();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            _ugsReady = true;
            Debug.Log($"[LobbyManager] UGS ready. Player ID: {AuthenticationService.Instance.PlayerId}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[LobbyManager] UGS init failed: {e.Message}");
            OnError?.Invoke("Could not reach online services. Check your connection.");
        }
    }

    void Update()
    {
        // Heartbeat keeps the lobby alive while the host is in session
        if (_currentLobby == null) return;
        _heartbeatTimer += Time.deltaTime;
        if (_heartbeatTimer < HeartbeatInterval) return;
        _heartbeatTimer = 0f;
        LobbyService.Instance.SendHeartbeatPingAsync(_currentLobby.Id);
    }

    void OnDestroy()
    {
        _ = CleanupLobby();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Creates a Relay allocation, starts as host, and creates a Lobby.</summary>
    public async void CreateLobby()
    {
        if (!CheckReady()) return;
        try
        {
            // 1. Relay allocation — host side
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(MaxPlayers - 1);
            string joinCode       = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            // 2. Hook transport to Relay
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(new RelayServerData(allocation, "dtls"));

            // 3. Start host (server + local client)
            NetworkManager.Singleton.StartHost();

            // 4. Create lobby (stores join code so friends can find it)
            var opts = new CreateLobbyOptions
            {
                IsPrivate = false,
                Data = new System.Collections.Generic.Dictionary<string, DataObject>
                {
                    { "RelayCode", new DataObject(DataObject.VisibilityOptions.Public, joinCode) }
                }
            };
            _currentLobby = await LobbyService.Instance.CreateLobbyAsync("GameCasual Room", MaxPlayers, opts);

            Debug.Log($"[LobbyManager] Hosting. Join code: {joinCode}");
            OnJoinCodeReady?.Invoke(joinCode);
            OnSessionStarted?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"[LobbyManager] CreateLobby failed: {e.Message}");
            OnError?.Invoke("Failed to create session.");
        }
    }

    /// <summary>Joins an existing relay session using the 6-char join code.</summary>
    public async void JoinLobby(string relayCode)
    {
        if (!CheckReady()) return;
        if (string.IsNullOrWhiteSpace(relayCode)) { OnError?.Invoke("Enter a join code."); return; }

        try
        {
            JoinAllocation join = await RelayService.Instance.JoinAllocationAsync(relayCode.Trim().ToUpper());

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(new RelayServerData(join, "dtls"));

            NetworkManager.Singleton.StartClient();

            Debug.Log($"[LobbyManager] Joined session with code: {relayCode}");
            OnSessionStarted?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"[LobbyManager] JoinLobby failed: {e.Message}");
            OnError?.Invoke("Invalid code or session not found.");
        }
    }

    /// <summary>Leaves the current session.</summary>
    public async void LeaveSession()
    {
        await CleanupLobby();
        NetworkManager.Singleton.Shutdown();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    bool CheckReady()
    {
        if (!_ugsReady)
        {
            OnError?.Invoke("Services not ready yet — wait a moment and try again.");
            return false;
        }
        if (NetworkManager.Singleton == null)
        {
            OnError?.Invoke("NetworkManager missing. Run Tools → Setup Multiplayer.");
            return false;
        }
        return true;
    }

    async Task CleanupLobby()
    {
        if (_currentLobby == null) return;
        try { await LobbyService.Instance.DeleteLobbyAsync(_currentLobby.Id); }
        catch { /* ignore on shutdown */ }
        _currentLobby = null;
    }
}
