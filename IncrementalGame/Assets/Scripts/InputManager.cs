using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InputManager : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    private Image imgJoystickBg;
    private Image imgJoystick;
    private Vector2 posInput;

    void Start()
    {
        imgJoystickBg = GetComponent<Image>();
        imgJoystick = transform.GetChild(0).GetComponent<Image>();
    }

    public void OnDrag(PointerEventData eventData)
    {

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(imgJoystickBg.rectTransform, eventData.position, eventData.pressEventCamera, out posInput))
        {
            posInput.x = posInput.x / (imgJoystickBg.rectTransform.sizeDelta.x);
            posInput.y = posInput.y / (imgJoystickBg.rectTransform.sizeDelta.y);

            if (posInput.magnitude > 1f)
            {
                posInput = posInput.normalized;
            }

            imgJoystick.rectTransform.anchoredPosition = new Vector2(posInput.x * (imgJoystickBg.rectTransform.sizeDelta.x / 4), posInput.y * (imgJoystickBg.rectTransform.sizeDelta.y / 4));
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        posInput = Vector2.zero;
        imgJoystick.rectTransform.anchoredPosition = Vector2.zero;
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
