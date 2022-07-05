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
    private ParticleSystem particlesStay;
    private float angle;
    public bool canStop;
    public Vector3 moveDirection;

    public CharacterController targetTransform;

    void Start()
    {
        player = this.GetComponent<CharacterController>();
        particles = this.transform.GetChild(1).GetComponent<ParticleSystem>();
        particlesStay = this.transform.GetChild(2).GetComponent<ParticleSystem>();
        targetTransform.transform.position = player.transform.position;
    }

    void Update()
    {

        if (player.velocity.magnitude <= 0)
        {
            particles.gameObject.SetActive(false);
            particlesStay.gameObject.SetActive(true);
        }

        else
        {
            particles.gameObject.SetActive(true);
            particlesStay.Clear();
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

        this.transform.rotation = Quaternion.Euler(new Vector3(0, angle, 0));

        if (speedUpgrade)
        {
            playerSpeed = 9;
        }

        if (targetTransform.velocity == Vector3.zero)
        {
            if (!_isStopped)
            {
                targetTransform.transform.position = player.transform.position;
                LeanTween.easeInOutBack(10, 25, 22);
                Debug.Log("STOP");
                _isStopped = true;
            }
        }

        else
        {
            _isStopped = false;
        }

        // LeanTween.move

        // LeanTween.move(player.gameObject, new Vector3(-horizontalMove, 0, -verticalMove) * playerSpeed, 0);
        // player.transform.position = Vector3.SmoothDamp(player.transform.position, new Vector3(-horizontalMove, 0, -verticalMove), playerSpeed * Time.deltaTime, 1, 11);

        // transform.position = Vector3.Lerp(transform.position, charPos, playerSpeed * Time.deltaTime);
        // player.transform.DOMove(charPos, 0);
        targetTransform.Move(new Vector3(-horizontalMove, 0, -verticalMove));

        Run();

    }

    void Run()
    {
        player.transform.position = Vector3.Lerp(transform.position, targetTransform.transform.position, playerSpeed * Time.deltaTime);

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
            if (currentMaterials == maxMaterials)
            {
                return;
            }

            currentMaterials++;
        }

        if (other.gameObject.CompareTag("UseMaterials"))
        {
            if (currentMaterials == minIndex)
            {
                return;
            }

            currentMaterials--;
            currentMaterialData.currentMaterialBuild++;

            currentMaterialData.maxMaterialsBuild = maxMaterials;
        }

        if (other.gameObject.CompareTag("UpgradeShop"))
        {
            // if (currentMaterials == minIndex)
            // {
            //     return;
            // }

            // currentMaterials--;
            // currentMaterialData.currentMaterialBuild++;

            // currentMaterialData.maxMaterialsBuild = maxMaterials;

            if (showed)
            {
                return;
            }

            GameManager.Instance.uiManager.ShowUpgradePanel(false);
            showed = true;
        }

    }

}
