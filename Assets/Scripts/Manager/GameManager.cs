using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public PlayerController playerController;
    public InputManager inputManager;
    public UIManager uiManager;
    public bool currentRotation = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // was Destroy(this) — that only destroys the component, not the duplicate GameObject
        }
        else
        {
            Instance = this;
        }
    }
}
