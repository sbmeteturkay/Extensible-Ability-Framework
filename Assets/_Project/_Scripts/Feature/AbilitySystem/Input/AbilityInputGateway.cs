using System;
using System.Collections.Generic;
using CaseStudy.Shared.AbilitySystem.Events;
using MessagePipe;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace CaseStudy.Feature.AbilitySystem.Input
{
    /// <summary>
    /// Publishes trigger requests from Input System actions.
    /// Input list order defines slot index.
    /// </summary>
    public sealed class AbilityInputGateway : MonoBehaviour
    {
        private readonly struct RegisteredBinding
        {
            public readonly InputAction Action;
            public readonly Action<InputAction.CallbackContext> Callback;

            public RegisteredBinding(InputAction action, Action<InputAction.CallbackContext> callback)
            {
                Action = action;
                Callback = callback;
            }
        }

        [Header("Input Actions")]
        [SerializeField] private List<InputActionReference> _actions = new(3);

        private readonly List<RegisteredBinding> _registeredBindings = new(4);
        private IPublisher<AbilityTriggerRequestedEvent> _triggerPublisher;

        [Inject]
        public void Construct(IPublisher<AbilityTriggerRequestedEvent> triggerPublisher)
        {
            _triggerPublisher = triggerPublisher;
        }

        private void OnEnable()
        {
            BindAll();
        }

        private void OnDisable()
        {
            UnbindAll();
        }

        private void BindAll()
        {
            UnbindAll();

            if (_actions == null)
            {
                return;
            }

            int count = _actions.Count;
            for (int slotIndex = 0; slotIndex < count; slotIndex++)
            {
                InputActionReference actionReference = _actions[slotIndex];
                if (actionReference == null || actionReference.action == null)
                {
                    continue;
                }

                int capturedSlotIndex = slotIndex;
                InputAction action = actionReference.action;
                Action<InputAction.CallbackContext> callback = _ => Publish(capturedSlotIndex);

                action.performed += callback;
                if (!action.enabled)
                {
                    action.Enable();
                }

                _registeredBindings.Add(new RegisteredBinding(action, callback));
            }
        }

        private void UnbindAll()
        {
            int count = _registeredBindings.Count;
            for (int i = 0; i < count; i++)
            {
                RegisteredBinding registered = _registeredBindings[i];
                if (registered.Action != null)
                {
                    registered.Action.performed -= registered.Callback;
                }
            }

            _registeredBindings.Clear();
        }

        private void Publish(int slotIndex)
        {
            if (_triggerPublisher == null || slotIndex < 0)
            {
                return;
            }

            _triggerPublisher.Publish(new AbilityTriggerRequestedEvent(slotIndex));
        }
    }
}
