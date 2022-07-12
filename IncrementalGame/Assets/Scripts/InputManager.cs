using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class InputManager : MonoBehaviour , IDragHandler, IPointerDownHandler, IPointerUpHandler 
{
    [SerializeField]
    private Image imgJoystickBg;
    [SerializeField]
    private Image imgJoystick;
    private Vector2 posInput;
    private Vector3 mousePos;

    public float movementSmoothing = 0.15f;
    [SerializeField]

    void Start()
    {
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(imgJoystickBg.rectTransform, eventData.position, eventData.pressEventCamera, out posInput))
        {
            posInput.x = posInput.x / (imgJoystickBg.rectTransform.sizeDelta.x) * 2;
            posInput.y = posInput.y / (imgJoystickBg.rectTransform.sizeDelta.y) * 2;

            if (posInput.magnitude > 1f)
            {
                posInput = posInput.normalized;
            }

            imgJoystick.rectTransform.anchoredPosition = new Vector2(posInput.x * (imgJoystickBg.rectTransform.sizeDelta.x / 4), posInput.y * (imgJoystickBg.rectTransform.sizeDelta.y / 4));
        }
    }

    void Update()
    {
        if (posInput == Vector2.zero)
        {
           GameManager.Instance.playerController.animator.SetBool("isRun", false);
        }

        else
        {
           GameManager.Instance.playerController.animator.SetBool("isRun", true);
            
        }
    }


    public void OnPointerDown(PointerEventData eventData)
    {
        LeanTween.alphaCanvas(imgJoystickBg.GetComponent<CanvasGroup>(), 1, .25f);
        GameManager.Instance.playerController.canStop = false;
        imgJoystickBg.GetComponent<RectTransform>().position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        OnDrag(eventData);
        GameManager.Instance.currentRotation = false;

    }

    public void OnPointerUp(PointerEventData eventData)
    {
        posInput = Vector2.zero;
        GameManager.Instance.playerController.canStop = true;
        LeanTween.alphaCanvas(imgJoystickBg.GetComponent<CanvasGroup>(), 0, .25f);
        LeanTween.move(imgJoystick.rectTransform, Vector2.zero, .2f).setEaseOutBounce();
        GameManager.Instance.currentRotation = true;

    }

    public float InputHorizontal()
    {
        if (posInput.x != 0)
            return posInput.x;
        else
            return Input.GetAxis("Horizontal");

    }

    public float InputVertical()
    {
        if (posInput.y != 0)
            return posInput.y;
        else
            return Input.GetAxis("Vertical");
    }

}
