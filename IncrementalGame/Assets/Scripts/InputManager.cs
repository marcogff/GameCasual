﻿using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class InputManager : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    private Image imgJoystickBg;
    private Image imgJoystick;
    private Vector2 posInput;
    private Vector3 mousePos;

    Vector2 movementInput = Vector2.zero;
    float forwardVelocity = 0;
    float sidewaysVelocity = 0;

    public float movementSmoothing = 0.15f;
    [SerializeField]

    void Start()
    {
        imgJoystickBg = GetComponent<Image>();
        imgJoystick = transform.GetChild(0).GetComponent<Image>();
        mousePos = Input.mousePosition;
    }

    public void OnDrag(PointerEventData eventData)
    {

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(imgJoystickBg.rectTransform, eventData.position, eventData.pressEventCamera, out posInput))
        {

            posInput.x = posInput.x / (imgJoystickBg.rectTransform.sizeDelta.x) * 4;
            posInput.y = posInput.y / (imgJoystickBg.rectTransform.sizeDelta.y) * 4;

            if (posInput.magnitude > 1f)
            {
                posInput = posInput.normalized;
            }

            imgJoystick.rectTransform.anchoredPosition = new Vector2(posInput.x * (imgJoystickBg.rectTransform.sizeDelta.x / 6), posInput.y * (imgJoystickBg.rectTransform.sizeDelta.y / 6));
        }
    }

    private void Movement()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            imgJoystickBg.transform.position = mousePos;
            imgJoystick.transform.position = mousePos;
        }
    }


    public void OnPointerDown(PointerEventData eventData)
    {
        GameManager.Instance.currentRotation = false;
        LeanTween.alphaCanvas(imgJoystick.rectTransform.parent.GetComponent<CanvasGroup>(), 1, .1f);
        OnDrag(eventData);
        GameManager.Instance.playerController.canStop = false;

    }

    public void OnPointerUp(PointerEventData eventData)
    {
        GameManager.Instance.currentRotation = true;
        posInput = Vector2.zero;
        LeanTween.alphaCanvas(imgJoystick.rectTransform.parent.GetComponent<CanvasGroup>(), .6f, .1f);
        LeanTween.move(imgJoystick.rectTransform, Vector2.zero, .5f).setEaseOutBounce();
        GameManager.Instance.playerController.canStop = true;
    }

    public float InputHorizontal()
    {
        if (posInput.x != 0)
            return posInput.x;
        else
            return Input.GetAxisRaw("Horizontal");

    }

    public float InputVertical()
    {
        if (posInput.y != 0)
            return posInput.y;
        else
            return Input.GetAxisRaw("Vertical");
    }

}
