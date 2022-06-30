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
    public MaterialsData currentMaterialData;

    void Start()
    {
        player = this.GetComponent<CharacterController>();
    }

    void Update()
    {
        horizontalMove = GameManager.Instance.inputManager.InputHorizontal();
        verticalMove = GameManager.Instance.inputManager.InputVertical();
    }

    void FixedUpdate()
    {
        player.Move(new Vector3(-horizontalMove, 0, -verticalMove) * playerSpeed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        currentMaterialData = other.gameObject.transform.parent.GetComponent<MaterialsData>();
    }

    void OnTriggerExit(Collider other)
    {
        currentMaterialData = null;
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
        
    }

    // void OnTriggerExit(Collider other)
    // {
        
    // }
}
