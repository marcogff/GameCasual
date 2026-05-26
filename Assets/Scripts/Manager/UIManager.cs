using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;


public class UIManager : MonoBehaviour
{
    public Image upgradePanel;
    [SerializeField] private CanvasGroup _panelCanvas;
    [SerializeField] private CanvasGroup _woodCanvas;
    [SerializeField] private CanvasGroup _fishCanvas;
    private TextMeshProUGUI _woodText;
    private TextMeshProUGUI _fishText;
    private bool _isDisplayedWood;
    private bool _isDisplayedFish;

    void Start()
    {
        _woodText = _woodCanvas.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        _fishText = _fishCanvas.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        SetUIText();
        ActivateUpgradeBtn();

        if (_isDisplayedWood)
        {
            _woodCanvas.gameObject.SetActive(true);
            _woodText.text = GameManager.Instance.playerController.currentElementsWood.Count.ToString();
            LeanTween.alphaCanvas(_woodCanvas, 1, .2f);

            _isDisplayedWood = false;
        }
        else
        {
            _woodCanvas.gameObject.SetActive(false);
        }

        if (_isDisplayedFish)
        {
            _fishCanvas.gameObject.SetActive(true);
            LeanTween.alphaCanvas(_fishCanvas, 1, .2f);
            _fishText.text = GameManager.Instance.playerController.currentElementsFish.Count.ToString();

            _isDisplayedFish = false;
        }

        else
        {
            _fishCanvas.gameObject.SetActive(false);
            
        }
    }

    private void SetUIText()
    {
        if (GameManager.Instance.playerController.hasMat)
        {
            LeanTween.alphaCanvas(_panelCanvas, 1, .2f);

            for (int i = 0; i < GameManager.Instance.playerController.currentElementsWood.Count; i++)
            {
                if (GameManager.Instance.playerController.currentElementsWood[i].name == "log(Clone)")
                {
                    _isDisplayedWood = true;
                }
            }

            for (int i = 0; i < GameManager.Instance.playerController.currentElementsFish.Count; i++)
            {
                if (GameManager.Instance.playerController.currentElementsFish[i].name == "Fish(Clone)")
                {
                    _isDisplayedFish = true;
                    Debug.Log("DISPLAYED");
                }
            }

        }

        else
        {
            LeanTween.alphaCanvas(_panelCanvas, 0, .2f);
        }
    }

    private void ActivateUpgradeBtn()
    {
        if (GameManager.Instance.playerController.currentElementsFish.Count <= 19)
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

        GameManager.Instance.playerController.currentElementsFish.RemoveRange(0, 20);

        upgradePanel.transform.GetChild(0).GetComponent<Image>().enabled = true;
        upgradePanel.transform.GetChild(0).GetChild(3).GetComponent<Image>().enabled = false;
        upgradePanel.transform.GetChild(0).GetChild(3).GetChild(0).GetComponent<TextMeshProUGUI>().text = "EQUIPPED";
    }

    private void FadeInElements()
    {
        LeanTween.alphaCanvas(upgradePanel.transform.GetChild(0).GetComponent<CanvasGroup>(), 1, 1f).setOnComplete(() =>
        LeanTween.scale(upgradePanel.transform.GetChild(0).GetChild(3).gameObject, new Vector3(.8f, .8f, .8f), .3f).setLoopPingPong()
            ); ;
    }

    private void FadeOutElements()
    {
        LeanTween.alphaCanvas(upgradePanel.transform.GetChild(0).GetComponent<CanvasGroup>(), 0, .3f);
    }

    public void ShowUpgradePanel(bool active)
    {
        active = !active;

        if (active)
        {
            LeanTween.moveX(upgradePanel.GetComponent<RectTransform>(), 0, .35f).setEaseOutBack().setOnComplete(() =>
            FadeInElements()
            );
        }

        else
        {
            LeanTween.moveX(upgradePanel.GetComponent<RectTransform>(), 1200, .35f).setEaseOutBack().setOnComplete(() =>
            FadeOutElements()
            );
        }
    }

    private void Fade(Image element, int endValue, float time)
    {
        element.DOFade(endValue, time);
    }

}
