using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;


public class UIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI coinsText;
    [SerializeField] private TextMeshProUGUI materialsText;
    [SerializeField] public Image upgradePanel;


    void Update()
    {
        SetUIText();
        ActivateUpgradeBtn();
    }

    private void SetUIText()
    {
        coinsText.text = GameManager.Instance.playerController.currentCoins.ToString();
        materialsText.text = GameManager.Instance.playerController.currentMaterials.ToString();
    }

    private void ActivateUpgradeBtn()
    {
        if (GameManager.Instance.playerController.currentCoins <= 120)
        {
            upgradePanel.transform.GetChild(0).GetChild(3).GetComponent<Button>().interactable = false;
            upgradePanel.transform.GetChild(0).GetChild(3).GetComponent<Animator>().enabled = false;
        }

        else
        {
            upgradePanel.transform.GetChild(0).GetChild(3).GetComponent<Button>().interactable = true;
            upgradePanel.transform.GetChild(0).GetChild(3).GetComponent<Animator>().enabled = true;
        }
    }

    public void MoreSpeed()
    {
        GameManager.Instance.playerController.speedUpgrade = true;

        upgradePanel.transform.GetChild(0).GetComponent<Image>().enabled = true;
        upgradePanel.transform.GetChild(0).GetChild(3).GetComponent<Image>().enabled = false;
        upgradePanel.transform.GetChild(0).GetChild(3).GetChild(0).GetComponent<TextMeshProUGUI>().text = "EQUIPPED";
        upgradePanel.transform.GetChild(0).GetChild(3).GetComponent<Animator>().enabled = false;
    }

    public void ShowUpgradePanel(bool active)
    {
        active = !active;

        upgradePanel.gameObject.SetActive(active);
        Fade(upgradePanel, 1, 2f);
    }

    private void Fade(Image element, int endValue, float time)
    {
        element.DOFade(endValue, time);
    }

}
