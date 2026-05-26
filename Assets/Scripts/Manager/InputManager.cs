using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class InputManager : MonoBehaviour , IDragHandler, IPointerDownHandler, IPointerUpHandler 
{
    [SerializeField]
    private Image _imgJoystickBg;
    [SerializeField]
    private Image _imgJoystick;
    private Vector2 _posInput;
    private Vector3 _mousePos;

    public float movementSmoothing = 0.15f;
    [SerializeField]

    void Start()
    {
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_imgJoystickBg.rectTransform, eventData.position, eventData.pressEventCamera, out _posInput))
        {
            _posInput.x = _posInput.x / (_imgJoystickBg.rectTransform.sizeDelta.x) * 2;
            _posInput.y = _posInput.y / (_imgJoystickBg.rectTransform.sizeDelta.y) * 2;

            if (_posInput.magnitude > 1f)
            {
                _posInput = _posInput.normalized;
            }

            _imgJoystick.rectTransform.anchoredPosition = new Vector2(_posInput.x * (_imgJoystickBg.rectTransform.sizeDelta.x / 4), _posInput.y * (_imgJoystickBg.rectTransform.sizeDelta.y / 4));
        }
    }

    void Update()
    {
        if (_posInput == Vector2.zero)
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
        LeanTween.alphaCanvas(_imgJoystickBg.GetComponent<CanvasGroup>(), 1, .25f);
        GameManager.Instance.playerController.canStop = false;
        _imgJoystickBg.GetComponent<RectTransform>().position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        OnDrag(eventData);
        GameManager.Instance.currentRotation = false;

    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _posInput = Vector2.zero;
        GameManager.Instance.playerController.canStop = true;
        LeanTween.alphaCanvas(_imgJoystickBg.GetComponent<CanvasGroup>(), 0, .25f);
        LeanTween.move(_imgJoystick.rectTransform, Vector2.zero, .2f).setEaseOutBounce();
        GameManager.Instance.currentRotation = true;

    }

    public float InputHorizontal()
    {
        if (_posInput.x != 0)
            return _posInput.x;
        else
            return Input.GetAxis("Horizontal");

    }

    public float InputVertical()
    {
        if (_posInput.y != 0)
            return _posInput.y;
        else
            return Input.GetAxis("Vertical");
    }

}
