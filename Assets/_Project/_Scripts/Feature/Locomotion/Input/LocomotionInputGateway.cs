using CaseStudy.Feature.Locomotion.Contracts;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CaseStudy.Feature.Locomotion.Input
{
    /// <summary>
    /// Caches movement input from Input System actions.
    /// This keeps movement logic independent from input source details.
    /// </summary>
    public sealed class LocomotionInputGateway : MonoBehaviour, ILocomotionInputReader
    {
        [Header("Input Actions")]
        [SerializeField] private InputActionReference _moveAction;

        private Vector2 _moveInput;

        public Vector2 MoveInput => _moveInput;

        private void OnEnable()
        {
            BindAction(_moveAction, OnMovePerformed, OnMoveCanceled);
        }

        private void OnDisable()
        {
            UnbindAction(_moveAction, OnMovePerformed, OnMoveCanceled);
            _moveInput = Vector2.zero;
        }

        public void SetMoveInput(Vector2 moveInput)
        {
            _moveInput = Vector2.ClampMagnitude(moveInput, 1f);
        }

        private void OnMovePerformed(InputAction.CallbackContext context)
        {
            SetMoveInput(context.ReadValue<Vector2>());
        }

        private void OnMoveCanceled(InputAction.CallbackContext context)
        {
            _moveInput = Vector2.zero;
        }

        private static void BindAction(
            InputActionReference actionReference,
            System.Action<InputAction.CallbackContext> performed,
            System.Action<InputAction.CallbackContext> canceled)
        {
            if (actionReference == null || actionReference.action == null)
            {
                return;
            }

            actionReference.action.performed += performed;
            actionReference.action.canceled += canceled;

            if (!actionReference.action.enabled)
            {
                actionReference.action.Enable();
            }
        }

        private static void UnbindAction(
            InputActionReference actionReference,
            System.Action<InputAction.CallbackContext> performed,
            System.Action<InputAction.CallbackContext> canceled)
        {
            if (actionReference == null || actionReference.action == null)
            {
                return;
            }

            actionReference.action.performed -= performed;
            actionReference.action.canceled -= canceled;
        }
    }
}
