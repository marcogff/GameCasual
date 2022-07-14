using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class PlayerController : MonoBehaviour
{
    private float horizontalMove;
    private float verticalMove;
    private CharacterController player;
    [Space(10)]
    [SerializeField] private int minIndex = 0;
    [HideInInspector] public bool speedUpgrade;
    private bool _isStopped;
    public MaterialsData currentMaterialData;
    private bool showed;
    private ParticleSystem particles;
    public float angle;
    public bool canStop;
    private int _playerCapacity = 100;
    bool instantiated = false;
    [HideInInspector]
    public Animator animator;

    public CharacterController targetTransform;

    public float playerAcceleration;
    [Range(0, 1)]
    public float dragFactor;
    public float gravity;
    private Transform bagPos;
    public List<GameObject> currentElementsWood = new List<GameObject>();
    public List<GameObject> currentElementsFish = new List<GameObject>();
    public bool hasMat;
    public GameObject temporalPrefab;
    bool _isEliminated;
    public int bagPosIndex;
    public GameObject particleSystemBreath;

    void Start()
    {
        player = this.GetComponent<CharacterController>();
        particles = this.transform.GetChild(1).GetComponent<ParticleSystem>();
        targetTransform.transform.position = player.transform.position;
        bagPos = this.transform.GetChild(2);
        animator = transform.GetChild(0).GetChild(0).GetChild(1).GetComponent<Animator>();
    }

    void Update()
    {
        if (speedUpgrade)
        {
            playerAcceleration = 240;
        }

        if (instantiated)
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
            particles.gameObject.SetActive(false);
        }

        else
        {
            particles.gameObject.SetActive(true);
        }

        horizontalMove = GameManager.Instance.inputManager.InputHorizontal();
        verticalMove = GameManager.Instance.inputManager.InputVertical();

        if (GameManager.Instance.currentRotation)
        {
            return;
        }

        angle = Mathf.Atan2(horizontalMove, verticalMove) * Mathf.Rad2Deg;

    }

    void FixedUpdate()
    {

        player.transform.rotation = Quaternion.Euler(new Vector3(0, angle, 0));

        Vector3 velocity = targetTransform.velocity;

        Vector3 input = new Vector3(
            -horizontalMove,
            0f,
            -verticalMove
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
        player.transform.position = Vector3.Lerp(player.transform.position, new Vector3(targetTransform.transform.position.x, player.transform.position.y, targetTransform.transform.position.z), .15f);
    }

    void OnTriggerEnter(Collider other)
    {
        currentMaterialData = other.gameObject.transform.parent.GetComponent<MaterialsData>();
    }

    void OnTriggerExit(Collider other)
    {
        currentMaterialData = null;

        if (other.gameObject.CompareTag("UpgradeShop"))
        {
            GameManager.Instance.uiManager.ShowUpgradePanel(true);
            showed = false;
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

                if (instantiated)
                {

                    return;
                }

                if (currentMaterialData.canDrop)
                {

                    GameObject log = Instantiate(currentMaterialData.materialData.prefab, currentMaterialData.spawnPoint.position, Quaternion.Euler(0, 90, 0), bagPos);

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

                if (instantiated)
                {

                    return;
                }

                if (currentMaterialData.canDrop)
                {


                    GameObject fish = Instantiate(currentMaterialData.materialData.prefab, currentMaterialData.spawnPoint.position, Quaternion.Euler(0, 90, 0), bagPos);
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

                if (currentElementsWood.Count == minIndex)
                {
                    currentElementsWood.Clear();

                    for (int i = 0; i < currentElementsWood.Count; i++)
                    {
                        Destroy(currentElementsWood[i]);
                    }

                    return;
                }

                if (instantiated)
                {
                    return;
                }

                currentMaterialData.spawnPoint = transform;

                GameObject log = Instantiate(currentMaterialData.materialData.prefab, currentMaterialData.spawnPoint.position, Quaternion.Euler(0, 90, 0), currentMaterialData.transform);

                RemoveFunc(log, "Wood");

            }

            if (other.gameObject.CompareTag("UpgradeShop"))
            {

                if (showed)
                {
                    return;
                }

                GameManager.Instance.uiManager.ShowUpgradePanel(false);
                showed = true;
            }
        }

    }

    void DeployElement(GameObject element, string type)
    {
        instantiated = true;
        currentMaterialData.currentElements++;
        bagPosIndex++;

        LeanTween.scale(element, new Vector3(2.5f, 2.5f, 2.5f), .1f).setEaseInBounce().setOnComplete(() =>

        LeanTween.move(element, bagPos, .15f).setEaseLinear().setOnComplete(() =>

        LeanTween.move(element, transform.position + new Vector3(0, 1, 0), .2f).setEaseLinear().setOnComplete(() =>
        CompleteFunc(element, currentMaterialData.materialData.vfx, true, type)
        )
        ));


    }

    void RemoveFunc(GameObject element, string type)
    {
        element.transform.position.Scale(new Vector3(1.5f, 1.5f, 1.5f));
        
        if (currentElementsWood.Count <= minIndex)
        {
            return;
        }
        if (currentMaterialData.elementsInBuild.Count >= currentMaterialData.maxMaterialsBuild)
        {
            return;
        }

        instantiated = true;

        LeanTween.scale(element, new Vector3(2.5f, 2.5f, 2.5f), .05f).setEaseInBounce();

        LeanTween.move(element, bagPos, .15f).setEaseLinear().setOnComplete(() =>

        LeanTween.move(element, currentMaterialData.transform.position, .2f).setEaseLinear().setOnComplete(() =>
        CompleteFunc(element, currentMaterialData.materialData.vfx, false, type)
        )
        );

    }

    void CompleteFunc(GameObject prefab, GameObject vfx, bool add, string type)
    {
        GameObject effect = Instantiate(vfx, prefab.transform.position, Quaternion.identity);
        Destroy(effect, .3f);
        prefab.SetActive(false);

        instantiated = false;

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
