using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    public MaterialsData currentMaterialData;
    private bool showed;
    private ParticleSystem particles;
    private ParticleSystem particlesStay;
    private float angle;

    void Start()
    {
        player = this.GetComponent<CharacterController>();
        particles = this.transform.GetChild(1).GetComponent<ParticleSystem>();
        particlesStay = this.transform.GetChild(2).GetComponent<ParticleSystem>();
    }

    void Update()
    {
        if(player.velocity.magnitude <= 0)
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

        player.Move(new Vector3(-horizontalMove, 0, -verticalMove) * playerSpeed * Time.deltaTime);
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
