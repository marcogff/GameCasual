using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InputManager : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private Image _imgJoystickBg;
    [SerializeField] private Image _imgJoystick;

    public float movementSmoothing = 0.15f;

    private Vector2 _posInput;
    private CanvasGroup _joystickBgCanvas;
    private static readonly int IsRunHash = Animator.StringToHash("isRun");

    void Start()
    {
        _joystickBgCanvas = _imgJoystickBg.GetComponent<CanvasGroup>();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_imgJoystickBg.rectTransform, eventData.position, eventData.pressEventCamera, out _posInput))
        {
            _posInput.x = _posInput.x / _imgJoystickBg.rectTransform.sizeDelta.x * 2;
            _posInput.y = _posInput.y / _imgJoystickBg.rectTransform.sizeDelta.y * 2;

            if (_posInput.magnitude > 1f)
                _posInput = _posInput.normalized;

            _imgJoystick.rectTransform.anchoredPosition = new Vector2(
                _posInput.x * (_imgJoystickBg.rectTransform.sizeDelta.x / 4),
                _posInput.y * (_imgJoystickBg.rectTransform.sizeDelta.y / 4));
        }
    }

    void Update()
    {
        // Guard against null during scene load or when the local player hasn't spawned yet
        var anim = GameManager.Instance?.playerController?.animator;
        if (anim != null)
            anim.SetBool(IsRunHash, _posInput != Vector2.zero);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        LeanTween.alphaCanvas(_joystickBgCanvas, 1, .25f);
        GameManager.Instance.playerController.canStop = false;
        _imgJoystickBg.rectTransform.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        OnDrag(eventData);
        GameManager.Instance.currentRotation = false;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _posInput = Vector2.zero;
        GameManager.Instance.playerController.canStop = true;
        LeanTween.alphaCanvas(_joystickBgCanvas, 0, .25f);
        LeanTween.move(_imgJoystick.rectTransform, Vector2.zero, .2f).setEaseOutBounce();
        GameManager.Instance.currentRotation = true;
    }

    public float InputHorizontal() => _posInput.x != 0 ? _posInput.x : Input.GetAxis("Horizontal");
    public float InputVertical() => _posInput.y != 0 ? _posInput.y : Input.GetAxis("Vertical");
}
