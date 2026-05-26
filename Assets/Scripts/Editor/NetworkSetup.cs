#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

/// <summary>
/// Phase 2 — Editor helper.
/// Tools → Setup Multiplayer  adds and configures the NetworkManager in the open scene.
///
/// What it does:
///   1. Creates a "NetworkManager" GameObject if one doesn't already exist.
///   2. Attaches NetworkManager + UnityTransport.
///   3. Marks the scene dirty so Unity saves the changes.
///   4. Displays next-step instructions.
/// </summary>
public static class NetworkSetup
{
    [MenuItem("Tools/Setup Multiplayer")]
    static void SetupMultiplayer()
    {
        // ── NetworkManager ────────────────────────────────────────────────────
#if UNITY_2022_2_OR_NEWER
        var nm = Object.FindFirstObjectByType<NetworkManager>();
#else
        var nm = Object.FindObjectOfType<NetworkManager>();
#endif

        if (nm == null)
        {
            var go = new GameObject("NetworkManager");
            nm = go.AddComponent<NetworkManager>();
            Debug.Log("[NetworkSetup] NetworkManager GameObject created.");
        }
        else
        {
            Debug.Log("[NetworkSetup] NetworkManager already present in scene — skipping creation.");
        }

        // ── UnityTransport ────────────────────────────────────────────────────
        var transport = nm.GetComponent<UnityTransport>();
        if (transport == null)
        {
            transport = nm.gameObject.AddComponent<UnityTransport>();
            Debug.Log("[NetworkSetup] UnityTransport added.");
        }

        // Relay requires DTLS (encrypted UDP). Set the protocol so the
        // transport is ready as soon as you call SetRelayServerData().
        transport.UseEncryption = true;

        // ── Scene bookkeeping ─────────────────────────────────────────────────
        EditorUtility.SetDirty(nm.gameObject);
        EditorSceneManager.MarkSceneDirty(nm.gameObject.scene);

        // ── Instructions ──────────────────────────────────────────────────────
        EditorUtility.DisplayDialog(
            "Multiplayer Setup Complete",
            "NetworkManager + UnityTransport are in the scene.\n\n" +
            "Required next steps:\n\n" +
            "① Player Prefab\n" +
            "   Select your player prefab and add:\n" +
            "   • NetworkObject\n" +
            "   • NetworkTransform  (syncs position/rotation)\n" +
            "   • PlayerNameTag\n" +
            "   Then drag it into NetworkManager → Player Prefab.\n\n" +
            "② Scene Objects (resources, build sites)\n" +
            "   Add NetworkObject to every GameObject that has MaterialsData.\n\n" +
            "③ Lobby\n" +
            "   Add LobbyManager + LobbyUI to a GameObject in the scene.\n\n" +
            "④ Unity Dashboard\n" +
            "   Enable Relay + Lobby services for this project ID.",
            "Got it!");
    }
}
#endif
