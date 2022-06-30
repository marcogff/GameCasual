using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


public class UIManager : MonoBehaviour
{
    [SerializeField]private TextMeshProUGUI coinsText;
    [SerializeField]private TextMeshProUGUI materialsText;


    void Update()
    {
        coinsText.text = GameManager.Instance.playerController.currentCoins.ToString();
        materialsText.text = GameManager.Instance.playerController.currentMaterials.ToString();
    }
}
