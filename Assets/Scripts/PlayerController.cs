using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Phase 2 multiplayer-aware player controller.
///
/// Networking behaviour:
/// • Extends NetworkBehaviour so NGO can spawn/track this object.
/// • IsLocalPlayer returns true when: no NetworkManager running (solo),
///   or this is the owning client — gates all input and interactions.
/// • NetworkVariable<int> NetWoodCount / NetFishCount — owner-writable,
///   readable by everyone so teammates can see each other's haul.
/// • OnNetworkSpawn disables cameras on remote player instances so only
///   the local player drives the camera.
/// • When NetworkManager is NOT running, everything behaves exactly as
///   before (IsLocalPlayer is always true, NetworkVariables are dormant).
/// </summary>
public class PlayerController : NetworkBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────
    public MaterialsData currentMaterialData;
    public GameObject particleSystemBreath;
    public float angle;
    public bool canStop;
    public int bagPosIndex;
    public CharacterController targetTransform;
    public float playerAcceleration;
    [Range(0, 1)] public float dragFactor;
    [Space(15)]
    public List<GameObject> currentElementsWood = new List<GameObject>();
    public List<GameObject> currentElementsFish = new List<GameObject>();
    [Space(15)]
    [SerializeField] private GameObject _worldCam;
    [SerializeField] private GameObject _caveCam;

    [HideInInspector] public bool hasMat;
    [HideInInspector] public GameObject temporalPrefab;
    [HideInInspector] public bool speedUpgrade;
    [HideInInspector] public Animator animator;

    // ── Network state ─────────────────────────────────────────────────────────
    /// <summary>How many wood items this player is carrying — visible to all clients.</summary>
    public readonly NetworkVariable<int> NetWoodCount = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    /// <summary>How many fish items this player is carrying — visible to all clients.</summary>
    public readonly NetworkVariable<int> NetFishCount = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    // ── Constants ─────────────────────────────────────────────────────────────
    private const float SpeedUpgradeAcceleration = 240f;
    private const float RunLerpFactor = 0.15f;
    private const float VfxLifetime = 0.3f;
    private const float PlayerYOffset = 1f;
    private const int   PlayerCapacity = 100;

    // Pickup animation — spring pop → arc up → drop to bag → squish → fly to player
    private const float PickupScaleSize          = 2.8f;
    private const float PickupScaleDuration      = 0.08f;
    private const float PickupArcHeight          = 1.8f;
    private const float PickupArcUpTime          = 0.10f;
    private const float PickupArcDownTime        = 0.13f;
    private const float PickupSquishScale        = 0.75f;
    private const float PickupSquishTime         = 0.04f;
    private const float PickupMoveToPlayerDuration = 0.09f;
    private const float PickupSpinDegrees        = 360f;

    // Deploy animation (depositing at build site)
    private const float DeployScaleDuration = 0.03f;
    private const float DeployMoveDuration  = 0.1f;

    // ── Private state ─────────────────────────────────────────────────────────
    private Transform _bagPos;
    private float _horizontalMove;
    private float _verticalMove;
    private bool  _isStopped;
    private bool  _showed;
    private ParticleSystem _particles;
    private bool  _instantiated;
    private CharacterController _player;

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// True when this instance should process input and interactions.
    /// Solo play: always true (NetworkManager not running).
    /// Multiplayer: true only on the owning client.
    /// </summary>
    private bool IsLocalPlayer =>
        NetworkManager.Singleton == null ||
        !NetworkManager.Singleton.IsListening ||
        IsOwner;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Start()
    {
        _player   = GetComponent<CharacterController>();
        _particles = transform.GetChild(1).GetComponent<ParticleSystem>();
        if (targetTransform != null)
            targetTransform.transform.position = _player.transform.position;
        _bagPos   = transform.GetChild(2);
        animator  = transform.GetChild(0).GetChild(0).GetChild(1).GetComponent<Animator>();
    }

    /// <summary>
    /// Called by NGO when this object is spawned on the network.
    /// Disables cameras on remote player instances so only the local player
    /// controls the camera.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            // Remote player — disable cameras that are wired to this prefab
            if (_worldCam != null) _worldCam.SetActive(false);
            if (_caveCam  != null) _caveCam.SetActive(false);
        }
    }

    void Update()
    {
        if (!IsLocalPlayer) return;

        if (speedUpgrade)
            playerAcceleration = SpeedUpgradeAcceleration;

        GameManager.Instance.inputManager.enabled = !_instantiated;

        hasMat = currentElementsWood.Count > 0 || currentElementsFish.Count > 0;

        if (_particles != null)
            _particles.gameObject.SetActive(targetTransform.velocity.magnitude > 0);

        _horizontalMove = GameManager.Instance.inputManager.InputHorizontal();
        _verticalMove   = GameManager.Instance.inputManager.InputVertical();

        if (!GameManager.Instance.currentRotation)
            angle = Mathf.Atan2(_horizontalMove, _verticalMove) * Mathf.Rad2Deg;
    }

    void FixedUpdate()
    {
        if (!IsLocalPlayer) return;

        _player.transform.rotation = Quaternion.Euler(0, angle, 0);

        Vector3 velocity = targetTransform.velocity;
        Vector3 input    = new Vector3(-_horizontalMove, 0f, -_verticalMove);
        velocity += input * playerAcceleration * Time.deltaTime;
        velocity *= dragFactor;
        targetTransform.Move(velocity * Time.deltaTime);

        bool isMoving = targetTransform.velocity != Vector3.zero;
        if (!isMoving && !_isStopped)
        {
            _isStopped = true;
            particleSystemBreath.SetActive(true);
        }
        else if (isMoving && _isStopped)
        {
            _isStopped = false;
            particleSystemBreath.SetActive(false);
        }

        Run();
    }

    void Run()
    {
        _player.transform.position = Vector3.Lerp(
            _player.transform.position,
            new Vector3(
                targetTransform.transform.position.x,
                _player.transform.position.y,
                targetTransform.transform.position.z),
            RunLerpFactor);
    }

    // ── Trigger interactions (local player only) ──────────────────────────────

    void OnTriggerEnter(Collider other)
    {
        if (!IsLocalPlayer) return;

        // Only update the tracked material when this collider actually belongs to a
        // MaterialsData object. Guards against null parents (e.g. the wolf's steal
        // trigger, which is a root object) — that was the NullReferenceException.
        Transform parent = other.transform.parent;
        if (parent != null)
        {
            var md = parent.GetComponent<MaterialsData>();
            if (md != null) currentMaterialData = md;
        }

        if (other.gameObject.CompareTag(Tags.Cave))
        {
            if (_worldCam != null) _worldCam.SetActive(false);
            if (_caveCam  != null) _caveCam.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsLocalPlayer) return;

        // Clear the tracked material only when leaving THAT material — so an unrelated
        // trigger (wolf, cave, shop) walking past doesn't wipe a valid resource ref.
        Transform parent = other.transform.parent;
        if (parent != null)
        {
            var md = parent.GetComponent<MaterialsData>();
            if (md != null && md == currentMaterialData) currentMaterialData = null;
        }

        if (other.gameObject.CompareTag(Tags.UpgradeShop))
        {
            GameManager.Instance.uiManager.ShowUpgradePanel(false);
            _showed = false;
        }

        if (other.gameObject.CompareTag(Tags.Cave))
        {
            if (_worldCam != null) _worldCam.SetActive(true);
            if (_caveCam  != null) _caveCam.SetActive(false);
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (!IsLocalPlayer) return;
        if (!_isStopped) return;

        if (other.gameObject.CompareTag(Tags.Materials))
        {
            if (currentElementsWood.Count >= PlayerCapacity || _instantiated) return;
            if (currentMaterialData == null || !currentMaterialData.canDrop) return;

            GameObject log = Instantiate(
                currentMaterialData.materialData.prefab,
                currentMaterialData.spawnPoint.position,
                Quaternion.Euler(0, 90, 0),
                _bagPos);
            temporalPrefab = log;
            DeployElement(log, MaterialType.Wood);
        }
        else if (other.gameObject.CompareTag(Tags.Fish))
        {
            if (currentElementsFish.Count >= PlayerCapacity || _instantiated) return;
            if (currentMaterialData == null || !currentMaterialData.canDrop) return;

            GameObject fish = Instantiate(
                currentMaterialData.materialData.prefab,
                currentMaterialData.spawnPoint.position,
                Quaternion.Euler(0, 90, 0),
                _bagPos);
            temporalPrefab = fish;
            DeployElement(fish, MaterialType.Fish);
        }
        else if (other.gameObject.CompareTag(Tags.UseMaterials))
        {
            if (currentMaterialData == null) return;
            if (currentMaterialData.elementsInBuild.Count >= currentMaterialData.maxMaterialsBuild) return;
            if (currentElementsWood.Count == 0 || _instantiated) return;

            currentMaterialData.spawnPoint = transform;
            GameObject log = Instantiate(
                currentMaterialData.materialData.prefab,
                currentMaterialData.spawnPoint.position,
                Quaternion.Euler(0, 90, 0),
                currentMaterialData.transform);
            RemoveFunc(log, MaterialType.Wood);
        }
        else if (other.gameObject.CompareTag(Tags.UpgradeShop))
        {
            if (_showed) return;
            GameManager.Instance.uiManager.ShowUpgradePanel(true);
            _showed = true;
        }
    }

    // ── Animation helpers ─────────────────────────────────────────────────────

    void DeployElement(GameObject element, MaterialType type)
    {
        _instantiated = true;
        currentMaterialData.currentElements++;
        bagPosIndex++;

        var capturedVfx = currentMaterialData.materialData.vfx;
        Vector3 arcPeak  = element.transform.position + Vector3.up * PickupArcHeight;
        Transform bagPos = _bagPos;

        LeanTween.scale(element, Vector3.one * PickupScaleSize, PickupScaleDuration)
            .setEaseOutBack()
            .setOnComplete(() =>
            {
                LeanTween.rotateAround(element, Vector3.up, PickupSpinDegrees,
                    PickupArcUpTime + PickupArcDownTime);

                LeanTween.move(element, arcPeak, PickupArcUpTime)
                    .setEaseOutQuad()
                    .setOnComplete(() =>

                        LeanTween.move(element, bagPos.position, PickupArcDownTime)
                            .setEaseInQuad()
                            .setOnComplete(() =>

                                LeanTween.scale(element, Vector3.one * PickupSquishScale, PickupSquishTime)
                                    .setEaseOutQuad()
                                    .setOnComplete(() =>

                                        LeanTween.move(element, transform.position + Vector3.up * PlayerYOffset,
                                            PickupMoveToPlayerDuration)
                                            .setEaseOutBack()
                                            .setOnComplete(() =>
                                                CompleteFunc(element, capturedVfx, true, type)))));
            });
    }

    void RemoveFunc(GameObject element, MaterialType type)
    {
        if (currentElementsWood.Count == 0 || currentMaterialData == null) return;
        if (currentMaterialData.elementsInBuild.Count >= currentMaterialData.maxMaterialsBuild) return;

        _instantiated = true;

        var capturedVfx      = currentMaterialData.materialData.vfx;
        var capturedBuildPos = currentMaterialData.transform.position;

        LeanTween.scale(element, new Vector3(24f, 7f, 11f), DeployScaleDuration)
            .setEaseInBounce()
            .setOnComplete(() =>
                LeanTween.move(element, capturedBuildPos, DeployMoveDuration)
                    .setEaseLinear()
                    .setOnComplete(() =>
                        CompleteFunc(element, capturedVfx, false, type)));
    }

    void CompleteFunc(GameObject prefab, GameObject vfx, bool add, MaterialType type)
    {
        if (vfx != null)
        {
            GameObject effect = Instantiate(vfx, prefab.transform.position, Quaternion.identity);
            Destroy(effect, VfxLifetime);
        }

        prefab.SetActive(false);
        _instantiated = false;

        if (type == MaterialType.Wood)
        {
            if (add)
            {
                currentElementsWood.Add(prefab);
            }
            else if (currentMaterialData != null && currentElementsWood.Count > 0)
            {
                currentMaterialData.elementsInBuild.Add(prefab);
                currentMaterialData.OnProgressAdded();
                Destroy(currentElementsWood[0], VfxLifetime);
                currentElementsWood.RemoveAt(0);
            }
        }
        else if (type == MaterialType.Fish)
        {
            if (add)
            {
                currentElementsFish.Add(prefab);
            }
            else if (currentMaterialData != null && currentElementsFish.Count > 0)
            {
                currentMaterialData.elementsInBuild.Add(prefab);
                currentMaterialData.OnProgressAdded();
                Destroy(currentElementsFish[0], VfxLifetime);
                currentElementsFish.RemoveAt(0);
            }
        }

        // Sync inventory counts so teammates can see our haul
        SyncInventoryNetwork();
    }

    /// <summary>
    /// Called by Enemy when it successfully steals resources from this player.
    /// Adjusts the visual bag-position index to match the reduced inventory.
    /// </summary>
    public void OnResourcesStolen(int count)
    {
        bagPosIndex = Mathf.Max(0, bagPosIndex - count);
        SyncInventoryNetwork();
    }

    /// <summary>
    /// Writes local wood/fish counts into the network variables so all
    /// clients can observe this player's inventory (e.g. for name-tag overlays).
    /// Only runs when actually connected — no-ops in solo play.
    /// </summary>
    void SyncInventoryNetwork()
    {
        if (!IsSpawned || !IsOwner) return;
        NetWoodCount.Value = currentElementsWood.Count;
        NetFishCount.Value = currentElementsFish.Count;
    }
}
