using System;
using System.Collections.Generic;
using CaseStudy.Shared.AbilitySystem.Events;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace CaseStudy.Feature.AbilitySystem.UI
{
    /// <summary>
    /// Bridges ability events to HUD updates and publishes slot click triggers.
    /// Scene-safe: depends on shared event stream, not player-side services.
    /// </summary>
    public sealed class AbilityHudPresenter : MonoBehaviour
    {
        private const float FALLBACK_INITIAL_MAX_ENERGY = 100f;

        [SerializeField] private AbilityHudView _view;

        private IPublisher<AbilityTriggerRequestedEvent> _triggerPublisher;
        private ISubscriber<AbilityLoadoutSlotAssignedEvent> _slotAssignedSubscriber;
        private ISubscriber<AbilityEnergyChangedEvent> _energySubscriber;
        private ISubscriber<AbilityCooldownStartedEvent> _cooldownStartedSubscriber;
        private ISubscriber<AbilityCooldownUpdatedEvent> _cooldownUpdatedSubscriber;
        private ISubscriber<AbilityCooldownCompletedEvent> _cooldownCompletedSubscriber;

        private readonly Dictionary<string, string> _slotKeyByAbilityKey = new(StringComparer.Ordinal);

        private IDisposable _slotAssignedSubscription;
        private IDisposable _energySubscription;
        private IDisposable _cooldownStartedSubscription;
        private IDisposable _cooldownUpdatedSubscription;
        private IDisposable _cooldownCompletedSubscription;

        [Inject]
        public void Construct(
            IPublisher<AbilityTriggerRequestedEvent> triggerPublisher,
            ISubscriber<AbilityLoadoutSlotAssignedEvent> slotAssignedSubscriber,
            ISubscriber<AbilityEnergyChangedEvent> energySubscriber,
            ISubscriber<AbilityCooldownStartedEvent> cooldownStartedSubscriber,
            ISubscriber<AbilityCooldownUpdatedEvent> cooldownUpdatedSubscriber,
            ISubscriber<AbilityCooldownCompletedEvent> cooldownCompletedSubscriber)
        {
            _triggerPublisher = triggerPublisher;
            _slotAssignedSubscriber = slotAssignedSubscriber;
            _energySubscriber = energySubscriber;
            _cooldownStartedSubscriber = cooldownStartedSubscriber;
            _cooldownUpdatedSubscriber = cooldownUpdatedSubscriber;
            _cooldownCompletedSubscriber = cooldownCompletedSubscriber;
        }

        private void Awake()
        {
            if (_view == null)
            {
                _view = GetComponent<AbilityHudView>();
            }
        }

        private void OnEnable()
        {
            if (_view == null)
            {
                Debug.LogWarning("AbilityHudPresenter: AbilityHudView reference is missing.");
                enabled = false;
                return;
            }

            if (_triggerPublisher == null
                || _slotAssignedSubscriber == null
                || _energySubscriber == null
                || _cooldownStartedSubscriber == null
                || _cooldownUpdatedSubscriber == null
                || _cooldownCompletedSubscriber == null)
            {
                Debug.LogWarning("AbilityHudPresenter: dependencies were not injected.");
                enabled = false;
                return;
            }

            _view.SetTriggerCallback(OnSlotClicked);

            _slotAssignedSubscription = _slotAssignedSubscriber.Subscribe(OnSlotAssigned);
            _energySubscription = _energySubscriber.Subscribe(OnEnergyChanged);
            _cooldownStartedSubscription = _cooldownStartedSubscriber.Subscribe(OnCooldownStarted);
            _cooldownUpdatedSubscription = _cooldownUpdatedSubscriber.Subscribe(OnCooldownUpdated);
            _cooldownCompletedSubscription = _cooldownCompletedSubscriber.Subscribe(OnCooldownCompleted);

            _view.SetEnergy(FALLBACK_INITIAL_MAX_ENERGY, FALLBACK_INITIAL_MAX_ENERGY);
        }

        private void OnDisable()
        {
            _slotAssignedSubscription?.Dispose();
            _energySubscription?.Dispose();
            _cooldownStartedSubscription?.Dispose();
            _cooldownUpdatedSubscription?.Dispose();
            _cooldownCompletedSubscription?.Dispose();

            _slotAssignedSubscription = null;
            _energySubscription = null;
            _cooldownStartedSubscription = null;
            _cooldownUpdatedSubscription = null;
            _cooldownCompletedSubscription = null;

            _view?.SetTriggerCallback(null);
        }

        private void OnSlotAssigned(AbilityLoadoutSlotAssignedEvent evt)
        {
            string abilityKey = NormalizeKey(evt.AbilityKey);
            string slotKey = NormalizeKey(evt.SlotKey);
            if (string.IsNullOrWhiteSpace(abilityKey) || string.IsNullOrWhiteSpace(slotKey))
            {
                return;
            }

            _slotKeyByAbilityKey[abilityKey] = slotKey;
            _view.BindSlot(slotKey, evt.Icon);
        }

        private void OnEnergyChanged(AbilityEnergyChangedEvent evt)
        {
            _view.SetEnergy(evt.CurrentEnergy, evt.MaxEnergy);
        }

        private void OnCooldownStarted(AbilityCooldownStartedEvent evt)
        {
            if (!_slotKeyByAbilityKey.TryGetValue(evt.AbilityKey, out string slotKey))
            {
                return;
            }

            _view.SetCooldown(slotKey, evt.DurationSeconds, 1f);
        }

        private void OnCooldownUpdated(AbilityCooldownUpdatedEvent evt)
        {
            if (!_slotKeyByAbilityKey.TryGetValue(evt.AbilityKey, out string slotKey))
            {
                return;
            }

            _view.SetCooldown(slotKey, evt.RemainingSeconds, evt.NormalizedRemaining);
        }

        private void OnCooldownCompleted(AbilityCooldownCompletedEvent evt)
        {
            if (!_slotKeyByAbilityKey.TryGetValue(evt.AbilityKey, out string slotKey))
            {
                return;
            }

            _view.ClearCooldown(slotKey);
        }

        private void OnSlotClicked(string slotKey)
        {
            slotKey = NormalizeKey(slotKey);
            if (string.IsNullOrWhiteSpace(slotKey))
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
