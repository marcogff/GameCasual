using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class PlayerController : MonoBehaviour
{
    private float horizontalMove;
    private float verticalMove;
    private CharacterController player;
    [SerializeField] private float playerSpeed;
    [HideInInspector] public int currentCoins = 0;
    [HideInInspector] public int currentMaterials = 0;
    [Space(10)]
    [SerializeField] private int maxCoins = 120;
    [SerializeField] private int maxMaterials = 200;
    [SerializeField] private int minIndex = 0;
    [HideInInspector] public bool speedUpgrade;
    private bool _isStopped;
    public MaterialsData currentMaterialData;
    private bool showed;
    private ParticleSystem particles;
    public float angle;
    public bool canStop;
    public Vector3 moveDirection;
    public float smooth;
    public int playerCapacity = 20;
    bool instantiated = false;
    [HideInInspector]
    public Animator animator;

    public CharacterController targetTransform;

    public float playerAcceleration;
    [Range(0, 1)]
    public float dragFactor;
    public float gravity;
    private Transform bagPos;
    public List<GameObject> currentElements = new List<GameObject>();
    int index = 0;

    public bool hasMat;
    public GameObject temporalPrefab;
    bool _isEliminated;


    void Start()
    {
        player = this.GetComponent<CharacterController>();
        particles = this.transform.GetChild(1).GetComponent<ParticleSystem>();
        targetTransform.transform.position = player.transform.position;
        bagPos = this.transform.GetChild(3);
        animator = transform.GetChild(0).GetChild(0).GetChild(1).GetComponent<Animator>();

    }

    void Update()
    {
        if (currentElements.Count >= 1)
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


        if (speedUpgrade)
        {
            playerSpeed = 9;
        }

        if (targetTransform.velocity == Vector3.zero)
        {
            if (!_isStopped)
            {
                // animator.SetBool("isRun", false);
                LeanTween.easeInOutBack(10, 25, 22);
                Debug.Log("STOP");
                _isStopped = true;
            }
        }

        else
        {
            _isStopped = false;
            // animator.SetBool("isRun", true);
        }

        Run();
    }

    void Run()
    {
        player.transform.position = Vector3.Lerp(player.transform.position, targetTransform.transform.position, .15f);
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

        if (other.gameObject.CompareTag("Coins"))
        {
            if (currentMaterialData.currentMaterialBuild == 0)
            {
                return;
            }

            if (currentCoins == maxCoins)
            {
                return;
            }

            currentCoins++;
        }

        if (other.gameObject.CompareTag("UseCoins"))
        {
            if (currentCoins == minIndex)
            {
                return;
            }

            currentCoins--;
        }

        if (other.gameObject.CompareTag("Materials"))
        {

            if (currentMaterials == maxMaterials || currentElements.Count >= 15)
            {
                return;
            }

            if (instantiated)
            {

                return;
            }

            if (_isStopped)
            {
                GameObject log = Instantiate(currentMaterialData.materialData.prefab, currentMaterialData.spawnPoint.position, Quaternion.Euler(0, 90, 0), bagPos);

                temporalPrefab = log;

                DeployElement(log);
                
            }


        }

        if (other.gameObject.CompareTag("UseMaterials"))
        {
            if (currentElements.Count == minIndex)
            {
                currentElements.Clear();

                for (int i = 0; i < currentElements.Count; i++)
                {
                    Destroy(currentElements[i]);
                }

                return;
            }

            if (instantiated)
            {
                return;
            }

            if (_isStopped)
            {
            currentMaterialData.spawnPoint = transform;

            GameObject log = Instantiate(currentMaterialData.materialData.prefab, currentMaterialData.spawnPoint.position, Quaternion.Euler(0, 90, 0), currentMaterialData.transform);

            RemoveFunc(log);
            }

        }

        if (other.gameObject.CompareTag("UpgradeShop"))
        {
            if (currentElements.Count == minIndex)
            {
                return;
            }

            if (showed)
            {
                return;
            }

            GameManager.Instance.uiManager.ShowUpgradePanel(false);
            showed = true;
        }



    }

    void DeployElement(GameObject element)
    {
        instantiated = true;

        LeanTween.scale(element, new Vector3(2.5f, 2.5f, 2.5f), .1f).setEaseInBounce().setOnComplete(() =>

        LeanTween.move(element, bagPos, .15f).setEaseLinear().setOnComplete(() =>

        LeanTween.move(element, transform.position + new Vector3(0, 1, 0), .2f).setEaseLinear().setOnComplete(() =>
        CompleteFunc(element, currentMaterialData.materialData.vfx, true)
        )
        ));

    }

    void RemoveFunc(GameObject prefab)
    {
        if (currentElements.Count == 0)
        {
            return;
        }

        instantiated = true;

        LeanTween.scale(prefab, new Vector3(2.5f, 2.5f, 2.5f), .1f).setEaseInBounce().setOnComplete(() =>

        LeanTween.move(prefab, bagPos, .15f).setEaseLinear().setOnComplete(() =>

        LeanTween.move(prefab, currentMaterialData.transform.position, .2f).setEaseLinear().setOnComplete(() =>
        CompleteFunc(prefab, currentMaterialData.materialData.vfx, false)
        )
        ));

    }

    void CompleteFunc(GameObject prefab, GameObject vfx, bool add)
    {
        // _isEliminated = false;

        GameObject effect = Instantiate(vfx, prefab.transform.position, Quaternion.identity);
        Destroy(effect, .3f);
        prefab.SetActive(false);

        instantiated = false;

        if (add)
        {
            currentElements.Add(prefab);
        }

        else
        {

            currentMaterialData.elementsInBuild.Add(prefab);
            Destroy(currentElements[0], .2f);
            currentElements.Remove(currentElements[0]);

            // if (currentMaterialData.elementsInBuild.Count == 15)
            // {
            //     // for (int i = 0; i < currentElements.Count; i++)
            //     // {
            //     // }
            // }
        }

    }


}
