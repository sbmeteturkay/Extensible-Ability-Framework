using CaseStudy.Feature.AbilitySystem.Domain;
using CaseStudy.Shared.AbilitySystem.Events;
using MessagePipe;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace CaseStudy.Feature.AbilitySystem.Input
{
    /// <summary>
    /// Publishes slot trigger requests from Input System actions.
    /// </summary>
    public sealed class AbilityInputGateway : MonoBehaviour
    {
        [Header("Input Actions")]
        [SerializeField] private InputActionReference _primaryAction;
        [SerializeField] private InputActionReference _secondaryAction;
        [SerializeField] private InputActionReference _utilityAction;

        private IPublisher<AbilityTriggerRequestedEvent> _triggerPublisher;

        [Inject]
        public void Construct(IPublisher<AbilityTriggerRequestedEvent> triggerPublisher)
        {
            _triggerPublisher = triggerPublisher;
        }

        private void OnEnable()
        {
            BindAction(_primaryAction, OnPrimaryPerformed);
            BindAction(_secondaryAction, OnSecondaryPerformed);
            BindAction(_utilityAction, OnUtilityPerformed);
        }

        private void OnDisable()
        {
            UnbindAction(_primaryAction, OnPrimaryPerformed);
            UnbindAction(_secondaryAction, OnSecondaryPerformed);
            UnbindAction(_utilityAction, OnUtilityPerformed);
        }

        private void OnPrimaryPerformed(InputAction.CallbackContext context)
        {
            Publish(AbilitySlot.Primary);
        }

        private void OnSecondaryPerformed(InputAction.CallbackContext context)
        {
            Publish(AbilitySlot.Secondary);
        }

        private void OnUtilityPerformed(InputAction.CallbackContext context)
        {
            Publish(AbilitySlot.Utility);
        }

        private void Publish(AbilitySlot slot)
        {
            if (_triggerPublisher == null)
            {
                return;
            }

            _triggerPublisher.Publish(new AbilityTriggerRequestedEvent(slot));
        }

        private static void BindAction(InputActionReference actionReference, System.Action<InputAction.CallbackContext> callback)
        {
            if (actionReference == null || actionReference.action == null)
            {
                return;
            }

            actionReference.action.performed += callback;
            if (!actionReference.action.enabled)
            {
                actionReference.action.Enable();
            }
        }

        private static void UnbindAction(InputActionReference actionReference, System.Action<InputAction.CallbackContext> callback)
        {
            if (actionReference == null || actionReference.action == null)
            {
                return;
            }

            actionReference.action.performed -= callback;
        }
    }
}