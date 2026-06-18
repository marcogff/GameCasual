using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ensures the self-contained manager objects (menu, pause, win, HUD, atmosphere)
/// exist after EVERY scene load — not just the first.
///
/// Why: [RuntimeInitializeOnLoadMethod] fires only once per app launch, so the
/// per-component AutoSpawn methods don't re-run when a "Restart / Play Again"
/// button reloads the scene. This bootstrapper hooks SceneManager.sceneLoaded and
/// re-creates anything missing, so those buttons work in standalone builds too.
/// </summary>
public static class GameBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        SpawnManagers(); // current (first) scene — sceneLoaded already fired for it
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => SpawnManagers();

    static void SpawnManagers()
    {
        Ensure<MainMenu>();
        Ensure<PauseMenu>();
        Ensure<WinScreen>();
        Ensure<MultiplayerHUD>();
        Ensure<DayNightCycle>();
    }

    static void Ensure<T>() where T : MonoBehaviour
    {
        if (Object.FindFirstObjectByType<T>() == null)
            new GameObject(typeof(T).Name).AddComponent<T>();
    }
}
