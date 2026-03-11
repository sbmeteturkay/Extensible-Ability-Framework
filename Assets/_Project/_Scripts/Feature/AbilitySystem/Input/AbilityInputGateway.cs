using CaseStudy.Feature.AbilitySystem.Domain;
using CaseStudy.Shared.AbilitySystem.Events;
using MessagePipe;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace CaseStudy.Feature.AbilitySystem.Input
{
    /// <summary>
    ///     Publishes slot trigger requests from keyboard and UI button callbacks.
    /// </summary>
    public sealed class AbilityInputGateway : MonoBehaviour
    {
        [Header("Keyboard Fallback")]
        [SerializeField] private KeyCode _primaryKey = KeyCode.Alpha1;

        [SerializeField] private KeyCode _secondaryKey = KeyCode.Alpha2;
        [SerializeField] private KeyCode _utilityKey = KeyCode.Alpha3;

        private IPublisher<AbilityTriggerRequestedEvent> _triggerPublisher;
        
        [Inject]
        public void Construct(IPublisher<AbilityTriggerRequestedEvent> triggerPublisher)
        {
            _triggerPublisher = triggerPublisher;
        }
        
        private void Update()
        {
            if (_triggerPublisher == null)
            {
                return;
            }

            if (Keyboard.current.qKey.isPressed)
            {
                TriggerPrimary();
            }

            if (Keyboard.current.wKey.isPressed)
            {
                TriggerSecondary();
            }

            if (Keyboard.current.eKey.isPressed)
            {
                TriggerUtility();
            }
        }



        public void TriggerPrimary()
        {
            Publish(AbilitySlot.Primary);
        }

        public void TriggerSecondary()
        {
            Publish(AbilitySlot.Secondary);
        }

        public void TriggerUtility()
        {
            Publish(AbilitySlot.Utility);
        }

        private void Publish(AbilitySlot slot)
        {
            if (_triggerPublisher == null)
            {
                return;
            }

            _triggerPublisher.Publish(new(slot));
        }
    }
}