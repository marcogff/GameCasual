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
    public LeanTweenType type;


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
            // upgradePanel.transform.GetChild(0).GetChild(3).GetComponent<Animator>().enabled = false;
        }

        else
        {
            upgradePanel.transform.GetChild(0).GetChild(3).GetComponent<Button>().interactable = true;
            // upgradePanel.transform.GetChild(0).GetChild(3).GetComponent<Animator>().enabled = true;
        }
    }

    public void MoreSpeed()
    {
        GameManager.Instance.playerController.speedUpgrade = true;

        upgradePanel.transform.GetChild(0).GetComponent<Image>().enabled = true;
        upgradePanel.transform.GetChild(0).GetChild(3).GetComponent<Image>().enabled = false;
        upgradePanel.transform.GetChild(0).GetChild(3).GetChild(0).GetComponent<TextMeshProUGUI>().text = "EQUIPPED";
        // upgradePanel.transform.GetChild(0).GetChild(3).GetComponent<Animator>().enabled = false;
    }
    private IEnumerator FadeInElements()
    {
        LeanTween.alphaCanvas(upgradePanel.transform.GetChild(0).GetComponent<CanvasGroup>(), 1, 1f);

        yield return new WaitForSeconds(.15f);

        upgradePanel.transform.GetChild(0).GetChild(3).GetComponent<Transform>().DOShakeRotation(.4f, 14, 10);
    }

    private void FadeOutElements()
    {
        LeanTween.alphaCanvas(upgradePanel.transform.GetChild(0).GetComponent<CanvasGroup>(), 0, .3f);
    }

    public void ShowUpgradePanel(bool active)
    {
        active = !active;

        if(active)
        {
            LeanTween.moveX(upgradePanel.GetComponent<RectTransform>(), 0, .35f).setEase(type).setOnComplete(()=> 
            StartCoroutine(FadeInElements())
            );
        }

        else
        {
            LeanTween.moveX(upgradePanel.GetComponent<RectTransform>(), 500, .35f).setEase(type).setOnComplete(()=> 
            FadeOutElements()
            );
        }
    }

    private void Fade(Image element, int endValue, float time)
    {
        element.DOFade(endValue, time);
    }

    // private void FadeText(TextMeshPro element, int endValue, float time)
    // {
    //     element.DOFade(endValue, time);
    // }

}
