using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;


public class UIManager : MonoBehaviour
{
    [SerializeField] private CanvasGroup panelCanvas;
    [SerializeField] private CanvasGroup woodCanvas;
    [SerializeField] private CanvasGroup fishCanvas;
    private TextMeshProUGUI _woodText;
    private TextMeshProUGUI _fishText;
    [SerializeField] public Image upgradePanel;
    public LeanTweenType type;
    private bool _isDisplayed;

    void Start()
    {
        _woodText = woodCanvas.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        _fishText = fishCanvas.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        SetUIText();
        ActivateUpgradeBtn();
        
        if (_isDisplayed)
        {
            LeanTween.alphaCanvas(woodCanvas, 1, .2f);
            _woodText.text = GameManager.Instance.playerController.currentElements.Count.ToString();

            _isDisplayed = false;
        }
    }

    private void SetUIText()
    {
        if (GameManager.Instance.playerController.hasMat)
        {
            LeanTween.alphaCanvas(panelCanvas, 1, .2f);

            for (int i = 0; i < GameManager.Instance.playerController.currentElements.Count; i++)
            {
                switch (GameManager.Instance.playerController.currentElements[i].name)
                {
                    case "log(Clone)":
                    _isDisplayed = true;
                    break;

                    // case "":
                    // _fishText.text = GameManager.Instance.playerController.currentElements.Count.ToString();
                    
                    // break;
                }
            }

        }

        else
        {
            LeanTween.alphaCanvas(panelCanvas, 0, .2f);
        }
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
