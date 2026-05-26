using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Core")]
    public PlayerController playerController;
    public InputManager inputManager;
    public UIManager uiManager;
    public bool currentRotation = false;

    [Header("Multiplayer")]
    /// <summary>
    /// Assign the LobbyManager component here (or on the same GameObject).
    /// Populated automatically if LobbyManager.Instance is ready.
    /// </summary>
    public LobbyManager lobbyManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        // Auto-wire lobby reference if not set in Inspector
        if (lobbyManager == null)
            lobbyManager = LobbyManager.Instance;
    }
}
