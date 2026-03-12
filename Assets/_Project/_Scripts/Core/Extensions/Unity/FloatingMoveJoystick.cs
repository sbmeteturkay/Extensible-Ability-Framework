using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch; 

// 2. ADIM: Eski Touch ile karışmaması için bunu ekle
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
/// <summary>
/// Completely standalone floating joystick view driven by a move InputAction.
/// When move input starts, joystick root snaps to pointer position and stays there until input is released.
/// </summary>
public sealed class FloatingMoveJoystick : MonoBehaviour
{
    private const float MIN_INPUT_SQR_MAGNITUDE = 0.0001f;

    [Header("Input")]
    [SerializeField] private InputActionReference _moveAction;

    [Header("UI References")]
    [SerializeField] private RectTransform _root;
    [SerializeField] private RectTransform _knob;

    [Header("Settings")]
    [SerializeField, Min(0f)] private float _knobMaxDistance = 80f;

    private Vector2 _initialRootAnchoredPosition;
    private Vector2 _initialKnobAnchoredPosition;
    private bool _isActive;

    private void Awake()
    {
        if (_root == null)
        {
            _root = transform as RectTransform;
        }

        if (_root != null)
        {
            _initialRootAnchoredPosition = _root.anchoredPosition;
        }

        if (_knob != null)
        {
            _initialKnobAnchoredPosition = _knob.anchoredPosition;
        }
    }

    private void OnEnable()
    {
        BindMoveAction();
    }

    private void OnDisable()
    {
        UnbindMoveAction();
        ResetJoystick();
    }

    private void LateUpdate()
    {
        if (!_isActive || _root == null || _knob == null)
        {
            return;
        }
        
        if (Touch.activeTouches.Count <= 0) return;
        
        UpdateKnobTowardsPointer();
    }

    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        Vector2 moveInput = context.ReadValue<Vector2>();
        if (moveInput.sqrMagnitude <= MIN_INPUT_SQR_MAGNITUDE)
        {
            return;
        }

        if (Touch.activeTouches.Count <= 0) return;
        if (!_isActive)
        {
            _isActive = true;
            SnapRootToPointer();
        }
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        ResetJoystick();
    }

    private void SnapRootToPointer()
    {
        if (_root == null)
        {
            return;
        }
        
        if (Touch.activeTouches.Count <= 0) return;

        RectTransform parentRect = _root.parent as RectTransform;
        if (parentRect == null)
        {
            return;
        }

        Vector2 pointerScreenPosition = ReadPointerScreenPosition();
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, pointerScreenPosition, null, out Vector2 localPoint))
        {
            return;
        }

        _root.anchoredPosition = localPoint;
    }

    private void UpdateKnobTowardsPointer()
    {
        Vector2 pointerScreenPosition = ReadPointerScreenPosition();
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, pointerScreenPosition, null, out Vector2 localPoint))
        {
            return;
        }

        _knob.anchoredPosition = Vector2.ClampMagnitude(localPoint, _knobMaxDistance);
    }

    private void ResetJoystick()
    {
        _isActive = false;

        if (_root != null)
        {
            _root.anchoredPosition = _initialRootAnchoredPosition;
        }

        if (_knob != null)
        {
            _knob.anchoredPosition = _initialKnobAnchoredPosition;
        }
    }

    private void BindMoveAction()
    {
        if (_moveAction == null || _moveAction.action == null)
        {
            return;
        }

        _moveAction.action.performed += OnMovePerformed;
        _moveAction.action.canceled += OnMoveCanceled;

        if (!_moveAction.action.enabled)
        {
            _moveAction.action.Enable();
        }
    }

    private void UnbindMoveAction()
    {
        if (_moveAction == null || _moveAction.action == null)
        {
            return;
        }

        _moveAction.action.performed -= OnMovePerformed;
        _moveAction.action.canceled -= OnMoveCanceled;
    }

    private static Vector2 ReadPointerScreenPosition()
    {
        if (Pointer.current != null)
        {
            return Pointer.current.position.ReadValue();
        }

        if (Mouse.current != null)
        {
            return Mouse.current.position.ReadValue();
        }

        if (Touchscreen.current != null)
        {
            return Touchscreen.current.primaryTouch.position.ReadValue();
        }

        return Vector2.zero;
    }
}
