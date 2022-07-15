using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

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
    [SerializeField] GameObject _worldCam;
    [SerializeField] GameObject _caveCam;
    [SerializeField] private int _minIndex = 0;


    [HideInInspector]public bool hasMat;
    [HideInInspector] public GameObject temporalPrefab;
    [HideInInspector] public bool speedUpgrade;
    [HideInInspector] public Animator animator;

    // Privates
    private Transform _bagPos;
    private float _horizontalMove;
    private float _verticalMove;
    private bool _isStopped;
    private bool _showed;
    private ParticleSystem _particles;
    private int _playerCapacity = 100;
    private bool _instantiated = false;
    private CharacterController _player;
    void Start()
    {
        _player = this.GetComponent<CharacterController>();
        _particles = this.transform.GetChild(1).GetComponent<ParticleSystem>();
        targetTransform.transform.position = _player.transform.position;
        _bagPos = this.transform.GetChild(2);
        animator = transform.GetChild(0).GetChild(0).GetChild(1).GetComponent<Animator>();
    }

    void Update()
    {
        if (speedUpgrade)
        {
            playerAcceleration = 240;
        }

        if (_instantiated)
        {
            GameManager.Instance.inputManager.enabled = false;
        }

        else
        {
            GameManager.Instance.inputManager.enabled = true;
        }

        if (currentElementsWood.Count >= 1 || currentElementsFish.Count >= 1)
        {
            hasMat = true;
        }


        else
        {
            hasMat = false;
        }

        if (targetTransform.velocity.magnitude <= 0)
        {
            _particles.gameObject.SetActive(false);
        }

        else
        {
            _particles.gameObject.SetActive(true);
        }

        _horizontalMove = GameManager.Instance.inputManager.InputHorizontal();
        _verticalMove = GameManager.Instance.inputManager.InputVertical();

        if (GameManager.Instance.currentRotation)
        {
            return;
        }

        angle = Mathf.Atan2(_horizontalMove, _verticalMove) * Mathf.Rad2Deg;

    }

    void FixedUpdate()
    {

        _player.transform.rotation = Quaternion.Euler(new Vector3(0, angle, 0));

        Vector3 velocity = targetTransform.velocity;

        Vector3 input = new Vector3(
            -_horizontalMove,
            0f,
            -_verticalMove
        );

        velocity += input * playerAcceleration * Time.deltaTime;
        velocity *= dragFactor;
        targetTransform.Move(velocity * Time.deltaTime);



        if (targetTransform.velocity == Vector3.zero)
        {
            if (!_isStopped)
            {
                _isStopped = true;
                particleSystemBreath.SetActive(true);
            }
        }

        else
        {
            _isStopped = false;
            particleSystemBreath.SetActive(false);
        }

        Run();
    }

    void Run()
    {
        _player.transform.position = Vector3.Lerp(_player.transform.position, new Vector3(targetTransform.transform.position.x, _player.transform.position.y, targetTransform.transform.position.z), .15f);
    }

    void OnTriggerEnter(Collider other)
    {
        currentMaterialData = other.gameObject.transform.parent.GetComponent<MaterialsData>();

        if (other.gameObject.CompareTag("Cave"))
        {
            _worldCam.SetActive(false);
            _caveCam.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        currentMaterialData = null;

        if (other.gameObject.CompareTag("UpgradeShop"))
        {
            GameManager.Instance.uiManager.ShowUpgradePanel(true);
            _showed = false;
        }

        if (other.gameObject.CompareTag("Cave"))
        {
            _worldCam.SetActive(true);
            _caveCam.SetActive(false);
        }

    }

    void OnTriggerStay(Collider other)
    {
        if (_isStopped)
        {
            if (other.gameObject.CompareTag("Materials"))
            {
                if (currentElementsWood.Count == _playerCapacity)
                {
                    return;
                }

                if (_instantiated)
                {

                    return;
                }

                if (currentMaterialData.canDrop)
                {

                    GameObject log = Instantiate(currentMaterialData.materialData.prefab, currentMaterialData.spawnPoint.position, Quaternion.Euler(0, 90, 0), _bagPos);

                    temporalPrefab = log;

                    DeployElement(log, "Wood");

                }
            }
            if (other.gameObject.CompareTag("Fish"))
            {
                if (currentElementsWood.Count == _playerCapacity)
                {
                    return;
                }

                if (_instantiated)
                {

                    return;
                }

                if (currentMaterialData.canDrop)
                {


                    GameObject fish = Instantiate(currentMaterialData.materialData.prefab, currentMaterialData.spawnPoint.position, Quaternion.Euler(0, 90, 0), _bagPos);
                    temporalPrefab = fish;

                    DeployElement(fish, "Fish");

                }
            }

            if (other.gameObject.CompareTag("UseMaterials"))
            {

                if (currentMaterialData.elementsInBuild.Count >= currentMaterialData.maxMaterialsBuild)
                {
                    return;
                }

                if (currentElementsWood.Count == _minIndex)
                {
                    currentElementsWood.Clear();

                    for (int i = 0; i < currentElementsWood.Count; i++)
                    {
                        Destroy(currentElementsWood[i]);
                    }

                    return;
                }

                if (_instantiated)
                {
                    return;
                }

                currentMaterialData.spawnPoint = transform;

                GameObject log = Instantiate(currentMaterialData.materialData.prefab, currentMaterialData.spawnPoint.position, Quaternion.Euler(0, 90, 0), currentMaterialData.transform);

                RemoveFunc(log, "Wood");

            }

            if (other.gameObject.CompareTag("UpgradeShop"))
            {

                if (_showed)
                {
                    return;
                }

                GameManager.Instance.uiManager.ShowUpgradePanel(false);
                _showed = true;
            }
        }

    }

    void DeployElement(GameObject element, string type)
    {
        _instantiated = true;
        currentMaterialData.currentElements++;
        bagPosIndex++;

        LeanTween.scale(element, new Vector3(2.5f, 2.5f, 2.5f), .1f).setOnComplete(() =>

        LeanTween.move(element, _bagPos, .15f).setEaseLinear().setOnComplete(() =>

        LeanTween.move(element, transform.position + new Vector3(0, 1, 0), .2f).setEaseLinear().setOnComplete(() =>
        CompleteFunc(element, currentMaterialData.materialData.vfx, true, type)
        )
        ));


    }

    void RemoveFunc(GameObject element, string type)
    {
        element.transform.position.Scale(new Vector3(1.5f, 1.5f, 1.5f));
        
        if (currentElementsWood.Count <= _minIndex)
        {
            return;
        }
        if (currentMaterialData.elementsInBuild.Count >= currentMaterialData.maxMaterialsBuild)
        {
            return;
        }

        _instantiated = true;

        LeanTween.scale(element, new Vector3(24f, 7f, 11f), .05f).setEaseInBounce().setOnComplete(() =>

        // LeanTween.move(element, bagPos, .15f).setEaseLinear().setOnComplete(() =>

        LeanTween.move(element, currentMaterialData.transform.position, .2f).setEaseLinear().setOnComplete(() =>
        CompleteFunc(element, currentMaterialData.materialData.vfx, false, type)
        ))
        ;

    }

    void CompleteFunc(GameObject prefab, GameObject vfx, bool add, string type)
    {
        GameObject effect = Instantiate(vfx, prefab.transform.position, Quaternion.identity);
        Destroy(effect, .3f);
        prefab.SetActive(false);

        _instantiated = false;

        if (type == "Wood")
        {
            if (add)
            {
                currentElementsWood.Add(prefab);
            }

            else
            {

                currentMaterialData.elementsInBuild.Add(prefab);
                Destroy(currentElementsWood[0], .2f);
                currentElementsWood.Remove(currentElementsWood[0]);

            }
        }

        if (type == "Fish")
        {
            
            if (add)
            {
                currentElementsFish.Add(prefab);
            }

            else
            {

                currentMaterialData.elementsInBuild.Add(prefab);
                Destroy(currentElementsFish[0], .2f);
                currentElementsWood.Remove(currentElementsWood[0]);

            }
        }

    }


}
