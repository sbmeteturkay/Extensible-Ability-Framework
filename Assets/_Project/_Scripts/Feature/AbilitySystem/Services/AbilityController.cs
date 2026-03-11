using System;
using System.Collections.Generic;
using System.Threading;
using CaseStudy.Feature.AbilitySystem.Contracts;
using CaseStudy.Feature.AbilitySystem.Data;
using CaseStudy.Feature.AbilitySystem.Domain;
using CaseStudy.Shared.AbilitySystem.Events;
using Cysharp.Threading.Tasks;
using MessagePipe;
using VContainer.Unity;

namespace CaseStudy.Feature.AbilitySystem.Services
{
    public sealed class AbilityController : IAbilityController, IStartable, IDisposable
    {
        private readonly IAbilityFactory _abilityFactory;
        private readonly ICooldownService _cooldownService;
        private readonly IEnergyService _energyService;
        private readonly ISubscriber<AbilityTriggerRequestedEvent> _triggerSubscriber;
        private readonly IPublisher<AbilityTriggeredEvent> _triggeredPublisher;
        private readonly IPublisher<AbilityExecutionFailedEvent> _executionFailedPublisher;

        private readonly Dictionary<AbilitySlot, IAbility> _slotToAbility = new();
        private readonly Dictionary<AbilityId, AbilityDataSO> _abilityDataById = new();

        private AbilityContext _context;
        private IDisposable _triggerSubscription;

        public AbilityController(
            IAbilityFactory abilityFactory,
            ICooldownService cooldownService,
            IEnergyService energyService,
            ISubscriber<AbilityTriggerRequestedEvent> triggerSubscriber,
            IPublisher<AbilityTriggeredEvent> triggeredPublisher,
            IPublisher<AbilityExecutionFailedEvent> executionFailedPublisher)
        {
            _abilityFactory = abilityFactory;
            _cooldownService = cooldownService;
            _energyService = energyService;
            _triggerSubscriber = triggerSubscriber;
            _triggeredPublisher = triggeredPublisher;
            _executionFailedPublisher = executionFailedPublisher;
        }

        public void Start()
        {
            _triggerSubscription = _triggerSubscriber.Subscribe(OnTriggerRequested);
        }

        public void Dispose()
        {
            _triggerSubscription?.Dispose();
            _triggerSubscription = null;
        }

        public void Configure(IReadOnlyDictionary<AbilitySlot, AbilityDataSO> loadout, AbilityContext context)
        {
            if (loadout == null)
            {
                throw new ArgumentNullException(nameof(loadout));
            }

            _context = context ?? throw new ArgumentNullException(nameof(context));

            _slotToAbility.Clear();
            _abilityDataById.Clear();

            foreach (KeyValuePair<AbilitySlot, AbilityDataSO> pair in loadout)
            {
                AbilityDataSO data = pair.Value;
                if (data == null)
                {
                    continue;
                }

                if (!_abilityFactory.TryCreate(data, out IAbility ability))
                {
                    continue;
                }

                ability.Initialize(_context, data);
                _slotToAbility[pair.Key] = ability;
                _abilityDataById[data.AbilityId] = data;
            }
        }

        public bool TryTrigger(AbilitySlot slot)
        {
            if (!_slotToAbility.TryGetValue(slot, out IAbility ability))
            {
                return false;
            }

            AbilityId abilityId = ability.Id;

            if (!_cooldownService.IsReady(abilityId))
            {
                _executionFailedPublisher.Publish(new AbilityExecutionFailedEvent(abilityId, AbilityFailureReason.CooldownActive));
                return false;
            }

            if (!_abilityDataById.TryGetValue(abilityId, out AbilityDataSO data))
            {
                _executionFailedPublisher.Publish(new AbilityExecutionFailedEvent(abilityId, AbilityFailureReason.InvalidConfiguration));
                return false;
            }

            if (!ability.CanExecute())
            {
                _executionFailedPublisher.Publish(new AbilityExecutionFailedEvent(abilityId, AbilityFailureReason.InvalidConfiguration));
                return false;
            }

            if (!_energyService.TryConsume(data.EnergyCost))
            {
                _executionFailedPublisher.Publish(new AbilityExecutionFailedEvent(abilityId, AbilityFailureReason.NotEnoughEnergy));
                return false;
            }

            ExecuteAbilityAsync(ability, data).Forget();
            return true;
        }

        public float GetCooldownRemaining(AbilityId abilityId)
        {
            return _cooldownService.GetRemaining(abilityId);
        }

        private void OnTriggerRequested(AbilityTriggerRequestedEvent evt)
        {
            TryTrigger(evt.Slot);
        }

        private async UniTaskVoid ExecuteAbilityAsync(IAbility ability, AbilityDataSO data)
        {
            try
            {
                await ability.ExecuteAsync(CancellationToken.None);
                _cooldownService.StartCooldown(ability.Id, data.CooldownSeconds);
                _triggeredPublisher.Publish(new AbilityTriggeredEvent(ability.Id));
            }
            catch
            {
                _energyService.Restore(data.EnergyCost);
                _executionFailedPublisher.Publish(new AbilityExecutionFailedEvent(ability.Id, AbilityFailureReason.InvalidConfiguration));
            }
        }
    }
}