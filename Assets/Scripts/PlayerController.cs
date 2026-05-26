using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
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

    // Animation
    private static readonly int IsRunHash = Animator.StringToHash("isRun");

    private const float SpeedUpgradeAcceleration = 240f;
    private const float RunLerpFactor = 0.15f;
    private const float VfxLifetime = 0.3f;
    private const float PlayerYOffset = 1f;
    private const int PlayerCapacity = 100;

    // Pickup animation — spring pop → arc up → drop to bag → squish → fly to player
    private const float PickupScaleSize    = 2.8f;   // initial pop size
    private const float PickupScaleDuration = 0.08f; // spring pop (setEaseOutBack)
    private const float PickupArcHeight    = 1.8f;   // world-units the item rises before falling to bag
    private const float PickupArcUpTime    = 0.10f;  // time rising
    private const float PickupArcDownTime  = 0.13f;  // time falling to bag
    private const float PickupSquishScale  = 0.75f;  // squish factor on landing
    private const float PickupSquishTime   = 0.04f;
    private const float PickupMoveToPlayerDuration = 0.09f;
    private const float PickupSpinDegrees  = 360f;   // full spin during arc

    // Deploy animation (depositing at build site)
    private const float DeployScaleDuration = 0.03f;
    private const float DeployMoveDuration  = 0.1f;

    private Transform _bagPos;
    private float _horizontalMove;
    private float _verticalMove;
    private bool _isStopped;
    private bool _showed;
    private ParticleSystem _particles;
    private bool _instantiated;
    private CharacterController _player;

    void Start()
    {
        _player = GetComponent<CharacterController>();
        _particles = transform.GetChild(1).GetComponent<ParticleSystem>();
        targetTransform.transform.position = _player.transform.position;
        _bagPos = transform.GetChild(2);
        animator = transform.GetChild(0).GetChild(0).GetChild(1).GetComponent<Animator>();
    }

    void Update()
    {
        if (speedUpgrade)
            playerAcceleration = SpeedUpgradeAcceleration;

        GameManager.Instance.inputManager.enabled = !_instantiated;

        hasMat = currentElementsWood.Count > 0 || currentElementsFish.Count > 0;

        _particles.gameObject.SetActive(targetTransform.velocity.magnitude > 0);

        _horizontalMove = GameManager.Instance.inputManager.InputHorizontal();
        _verticalMove = GameManager.Instance.inputManager.InputVertical();

        if (!GameManager.Instance.currentRotation)
            angle = Mathf.Atan2(_horizontalMove, _verticalMove) * Mathf.Rad2Deg;
    }

    void FixedUpdate()
    {
        _player.transform.rotation = Quaternion.Euler(0, angle, 0);

        Vector3 velocity = targetTransform.velocity;
        Vector3 input = new Vector3(-_horizontalMove, 0f, -_verticalMove);
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
            new Vector3(targetTransform.transform.position.x, _player.transform.position.y, targetTransform.transform.position.z),
            RunLerpFactor);
    }

    void OnTriggerEnter(Collider other)
    {
        currentMaterialData = other.gameObject.transform.parent.GetComponent<MaterialsData>();

        if (other.gameObject.CompareTag(Tags.Cave))
        {
            _worldCam.SetActive(false);
            _caveCam.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        currentMaterialData = null;

        if (other.gameObject.CompareTag(Tags.UpgradeShop))
        {
            GameManager.Instance.uiManager.ShowUpgradePanel(false);
            _showed = false;
        }

        if (other.gameObject.CompareTag(Tags.Cave))
        {
            _worldCam.SetActive(true);
            _caveCam.SetActive(false);
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (!_isStopped) return;

        if (other.gameObject.CompareTag(Tags.Materials))
        {
            // BUG FIX: was checking currentElementsWood for both wood AND fish capacity
            if (currentElementsWood.Count >= PlayerCapacity || _instantiated) return;
            if (currentMaterialData == null || !currentMaterialData.canDrop) return;

            GameObject log = Instantiate(currentMaterialData.materialData.prefab, currentMaterialData.spawnPoint.position, Quaternion.Euler(0, 90, 0), _bagPos);
            temporalPrefab = log;
            DeployElement(log, MaterialType.Wood);
        }
        else if (other.gameObject.CompareTag(Tags.Fish))
        {
            // BUG FIX: was checking currentElementsWood.Count instead of currentElementsFish.Count
            if (currentElementsFish.Count >= PlayerCapacity || _instantiated) return;
            if (currentMaterialData == null || !currentMaterialData.canDrop) return;

            GameObject fish = Instantiate(currentMaterialData.materialData.prefab, currentMaterialData.spawnPoint.position, Quaternion.Euler(0, 90, 0), _bagPos);
            temporalPrefab = fish;
            DeployElement(fish, MaterialType.Fish);
        }
        else if (other.gameObject.CompareTag(Tags.UseMaterials))
        {
            if (currentMaterialData == null) return;
            if (currentMaterialData.elementsInBuild.Count >= currentMaterialData.maxMaterialsBuild) return;

            // BUG FIX: was Clear()ing then looping over the now-empty list — Destroy never ran
            if (currentElementsWood.Count == 0 || _instantiated) return;

            currentMaterialData.spawnPoint = transform;
            GameObject log = Instantiate(currentMaterialData.materialData.prefab, currentMaterialData.spawnPoint.position, Quaternion.Euler(0, 90, 0), currentMaterialData.transform);
            RemoveFunc(log, MaterialType.Wood);
        }
        else if (other.gameObject.CompareTag(Tags.UpgradeShop))
        {
            if (_showed) return;
            GameManager.Instance.uiManager.ShowUpgradePanel(true);
            _showed = true;
        }
    }

    void DeployElement(GameObject element, MaterialType type)
    {
        _instantiated = true;
        currentMaterialData.currentElements++;
        bagPosIndex++;

        var capturedVfx = currentMaterialData.materialData.vfx;
        Vector3 arcPeak = element.transform.position + Vector3.up * PickupArcHeight;
        Transform bagPos = _bagPos; // capture ref in case bag moves

        // 1. Spring pop — overshoots then settles (setEaseOutBack gives the satisfying bounce)
        LeanTween.scale(element, Vector3.one * PickupScaleSize, PickupScaleDuration)
            .setEaseOutBack()
            .setOnComplete(() =>
            {
                // Full spin during the whole arc (start it before the move so timing aligns)
                LeanTween.rotateAround(element, Vector3.up, PickupSpinDegrees, PickupArcUpTime + PickupArcDownTime);

                // 2. Rise to arc peak
                LeanTween.move(element, arcPeak, PickupArcUpTime)
                    .setEaseOutQuad()
                    .setOnComplete(() =>

                        // 3. Fall into bag
                        LeanTween.move(element, bagPos.position, PickupArcDownTime)
                            .setEaseInQuad()
                            .setOnComplete(() =>

                                // 4. Squish on land, then fly to player body
                                LeanTween.scale(element, Vector3.one * PickupSquishScale, PickupSquishTime)
                                    .setEaseOutQuad()
                                    .setOnComplete(() =>

                                        LeanTween.move(element, transform.position + Vector3.up * PlayerYOffset, PickupMoveToPlayerDuration)
                                            .setEaseOutBack()
                                            .setOnComplete(() => CompleteFunc(element, capturedVfx, true, type)))));
            });
    }

    void RemoveFunc(GameObject element, MaterialType type)
    {
        if (currentElementsWood.Count == 0 || currentMaterialData == null) return;
        if (currentMaterialData.elementsInBuild.Count >= currentMaterialData.maxMaterialsBuild) return;

        _instantiated = true;

        // Capture before async — currentMaterialData may change while tweens are running
        var capturedVfx = currentMaterialData.materialData.vfx;
        var capturedBuildPos = currentMaterialData.transform.position;

        LeanTween.scale(element, new Vector3(24f, 7f, 11f), DeployScaleDuration).setEaseInBounce().setOnComplete(() =>
            LeanTween.move(element, capturedBuildPos, DeployMoveDuration).setEaseLinear().setOnComplete(() =>
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
                Destroy(currentElementsFish[0], VfxLifetime);
                currentElementsFish.RemoveAt(0); // BUG FIX: was wrongly removing from wood list
            }
        }
    }
}
