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

    void Start()
    {
        player = this.GetComponent<CharacterController>();
    }

    void Update()
    {
        horizontalMove = GameManager.Instance.inputManager.InputHorizontal();
        verticalMove = GameManager.Instance.inputManager.InputVertical();

        // if(player.velocity.magnitude <= 0)
        // {
        //     Debug.Log("STOP");
        // }
    }

    void FixedUpdate()
    {
        if (speedUpgrade)
        {
            playerSpeed = 9;
        }
        // Vector3 currentPos = new Vector3(-horizontalMove, 0, -verticalMove);

        // LeanTween.move(player.gameObject, new Vector3(-horizontalMove, 0, -verticalMove), .1f);

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
