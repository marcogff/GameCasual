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
        }

        else
        {
            upgradePanel.transform.GetChild(0).GetChild(3).GetComponent<Button>().interactable = true;
        }
    }

    public void MoreSpeed()
    {
        GameManager.Instance.playerController.speedUpgrade = true;

        upgradePanel.transform.GetChild(0).GetComponent<Image>().enabled = true;
        upgradePanel.transform.GetChild(0).GetChild(3).GetComponent<Image>().enabled = false;
        upgradePanel.transform.GetChild(0).GetChild(3).GetChild(0).GetComponent<TextMeshProUGUI>().text = "EQUIPPED";
    }
    private void FadeInElements()
    {
        LeanTween.alphaCanvas(upgradePanel.transform.GetChild(0).GetComponent<CanvasGroup>(), 1, 1f).setOnComplete(()=> 
            StartCoroutine(RepeatAnim())
            );;
    }
    private IEnumerator RepeatAnim()
    {

        upgradePanel.transform.GetChild(0).GetChild(3).GetComponent<Transform>().DOShakeRotation(.6f, 30, 25, 25);

        yield return new WaitForSeconds(3f);

        yield return StartCoroutine(RepeatAnim());
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
            FadeInElements()
            );
        }

        else
        {
            LeanTween.moveX(upgradePanel.GetComponent<RectTransform>(), 1200, .35f).setEase(type).setOnComplete(()=> 
            FadeOutElements()
            );
        }
    }

    private void Fade(Image element, int endValue, float time)
    {
        element.DOFade(endValue, time);
    }

}
