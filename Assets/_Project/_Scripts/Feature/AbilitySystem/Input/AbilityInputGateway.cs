using System;
using System.Collections.Generic;
using CaseStudy.Feature.AbilitySystem.Data;
using CaseStudy.Shared.AbilitySystem.Events;
using MessagePipe;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace CaseStudy.Feature.AbilitySystem.Input
{
    /// <summary>
    /// Publishes slot trigger requests from Input System actions.
    /// Supports any number of slot bindings.
    /// </summary>
    public sealed class AbilityInputGateway : MonoBehaviour
    {
        [Serializable]
        private sealed class InputSlotBinding
        {
            [SerializeField] private SlotDefinitionSO _slot;
            [SerializeField] private InputActionReference _action;

            public SlotDefinitionSO Slot => _slot;

            public string SlotKey => _slot != null ? NormalizeKey(_slot.SlotKey) : string.Empty;

            public InputActionReference Action => _action;

            private static string NormalizeKey(string key)
            {
                return string.IsNullOrWhiteSpace(key) ? string.Empty : key.Trim();
            }
        }

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

        [Header("Input Bindings")]
        [SerializeField] private List<InputSlotBinding> _bindings = new(3);

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

            if (_bindings == null)
            {
                return;
            }

            int count = _bindings.Count;
            for (int i = 0; i < count; i++)
            {
                InputSlotBinding binding = _bindings[i];
                if (binding == null || binding.Slot == null || string.IsNullOrWhiteSpace(binding.SlotKey) || binding.Action == null || binding.Action.action == null)
                {
                    continue;
                }

                string slotKey = binding.SlotKey;
                InputAction action = binding.Action.action;
                Action<InputAction.CallbackContext> callback = _ => Publish(slotKey);

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

        private void Publish(string slotKey)
        {
            slotKey = NormalizeKey(slotKey);
            if (_triggerPublisher == null || string.IsNullOrWhiteSpace(slotKey))
            {
                return;
            }

            _triggerPublisher.Publish(new AbilityTriggerRequestedEvent(slotKey));
        }

        private static string NormalizeKey(string key)
        {
            return string.IsNullOrWhiteSpace(key) ? string.Empty : key.Trim();
        }
    }
}
