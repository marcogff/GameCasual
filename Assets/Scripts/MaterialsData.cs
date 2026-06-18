using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using TMPro;

/// <summary>
/// Phase 3 — Networked resource node and build-site data.
///
/// Networking behaviour:
/// • Extends NetworkBehaviour so NGO tracks this scene object.
///   ► Add a NetworkObject component to every MaterialsData GameObject in the scene
///     for multiplayer sync. Solo play works without it (IsSpawned == false path).
/// • NetCanDrop  — server-authoritative flag; clients read it to see availability.
/// • NetBuildProgress — server-authoritative build count; all clients display it.
/// • Fill() coroutine (respawn timer) only runs on the server so the cooldown is
///   consistent; the result propagates automatically via NetworkVariable.
/// </summary>
public class MaterialsData : NetworkBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────
    public int maxMaterialsBuild;
    public GameObject obj;
    public MaterialsSO materialData;
    public Transform spawnPoint;
    public List<GameObject> elementsInBuild = new List<GameObject>();
    public GameObject prefabLand;
    public BoxCollider limit;
    public TextMeshProUGUI currentText;
    public TextMeshProUGUI needText;
    public GameObject parentText;
    public bool dropItems;

    // ── Networked state ───────────────────────────────────────────────────────
    /// <summary>Server controls availability; all clients observe.</summary>
    public readonly NetworkVariable<bool> NetCanDrop = new NetworkVariable<bool>(
        true,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    /// <summary>Server tracks build progress; all clients display it.</summary>
    public readonly NetworkVariable<int> NetBuildProgress = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // ── Local state (mirrors / fallback for solo) ─────────────────────────────
    /// <summary>
    /// Whether this resource can currently be collected.
    /// Multiplayer: synced via NetCanDrop. Solo: local field.
    /// </summary>
    public bool canDrop
    {
        get => IsSpawned ? NetCanDrop.Value : _canDropLocal;
        set
        {
            if (IsSpawned)
            {
                if (IsServer) NetCanDrop.Value = value;
            }
            else
            {
                _canDropLocal = value;
            }
        }
    }

    public int currentElements = 10;

    private bool _canDropLocal  = true;
    private bool _currentDeployed;
    private bool _coroutineExecuted;
    private GameObject _indicatorObject;

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// True when this machine should run authoritative logic
    /// (the Fill respawn timer, build deploy).
    /// Solo: always true. Multiplayer: only on the host/server.
    /// </summary>
    private bool IsServerSide => !IsSpawned || IsServer;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Start()
    {
        if (dropItems)
        {
            LeanTween.scale(parentText, new Vector3(1.2f, 1.2f, 1.2f), .3f).setLoopPingPong();
            _indicatorObject = transform.GetChild(0).GetChild(0).GetChild(1).gameObject;
        }
    }

    public override void OnNetworkSpawn()
    {
        // When this node is spawned over the network, mirror the server's
        // current canDrop value into the local indicator immediately.
        _canDropLocal = NetCanDrop.Value;
    }

    void Update()
    {
        if (dropItems && _indicatorObject != null)
            _indicatorObject.SetActive(canDrop);

        if (currentText != null && needText != null)
        {
            // Prefer the networked build count on clients; fall back to local list count
            int displayed = IsSpawned ? NetBuildProgress.Value : elementsInBuild.Count;
            currentText.text = displayed.ToString();
            needText.text    = maxMaterialsBuild.ToString();
        }

        // Respawn timer — only server decides when resources come back
        if (currentElements == 10 && dropItems && !_coroutineExecuted && IsServerSide)
            StartCoroutine(Fill(7));

        // Build completion — also only server triggers this
        if (IsServerSide && prefabLand != null && elementsInBuild.Count == maxMaterialsBuild)
        {
            if (parentText != null)
            {
                LeanTween.scale(parentText, Vector3.zero, .2f);
                Destroy(transform.GetChild(0).gameObject);
                Destroy(parentText);
                parentText = null;
            }

            if (_currentDeployed)
            {
                for (int i = 0; i < elementsInBuild.Count; i++)
                    Destroy(elementsInBuild[i]);
                elementsInBuild.Clear();
                return;
            }

            DeployLand();
        }
    }

    /// <summary>
    /// Called by PlayerController immediately after a resource is deposited here.
    /// Plays a bounce on the progress text and syncs the network variable.
    /// </summary>
    public void OnProgressAdded()
    {
        // Sync networked build count
        if (IsSpawned && IsServer)
            NetBuildProgress.Value = elementsInBuild.Count;

        // Bounce the progress label so the player gets immediate feedback
        if (parentText == null) return;
        LeanTween.cancel(parentText);
        parentText.transform.localScale = Vector3.one;
        LeanTween.scale(parentText, Vector3.one * 1.4f, 0.08f)
            .setEaseOutBack()
            .setOnComplete(() =>
                LeanTween.scale(parentText, Vector3.one * 1.2f, 0.12f).setEaseOutQuad());
    }

    // ── Private ───────────────────────────────────────────────────────────────

    void DeployLand()
    {
        limit.enabled    = false;
        _currentDeployed = true;

        // Big bounce-in on the unlocked land patch
        prefabLand.transform.localScale = Vector3.zero;
        LeanTween.scale(prefabLand, Vector3.one * 1.15f, 0.3f).setEaseOutBack()
            .setOnComplete(() =>
                LeanTween.scale(prefabLand, Vector3.one, 0.18f).setEaseOutQuad());

        // Green screen flash so the player knows something big just happened
        StealEffect.Instance.Celebrate();
    }

    private IEnumerator Fill(int time)
    {
        _coroutineExecuted = true;
        canDrop            = false;   // writes NetCanDrop on server, _canDropLocal in solo
        currentElements    = 10;

        LeanTween.cancel(obj);
        LeanTween.scale(obj, Vector3.one * 1.3f, 0.08f).setEaseOutQuad().setOnComplete(() =>
        {
            LeanTween.rotateAround(obj, Vector3.up, 180f, 0.2f);
            LeanTween.scale(obj, Vector3.zero, 0.22f).setEaseInBack();
        });

        yield return new WaitForSeconds(time);

        // Resource respawned
        obj.transform.localScale = Vector3.zero;
        LeanTween.rotateAround(obj, Vector3.up, 360f, 0.45f).setEaseOutQuad();
        LeanTween.scale(obj, Vector3.one, 0.45f).setEaseOutBack();

        canDrop            = true;
        _coroutineExecuted = false;
        currentElements    = 0;
    }
}
