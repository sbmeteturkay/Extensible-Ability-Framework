using System;
using System.Collections.Generic;
using System.Threading;
using CaseStudy.Feature.AbilitySystem.Contracts;
using CaseStudy.Feature.AbilitySystem.Data;
using CaseStudy.Feature.AbilitySystem.Domain;
using CaseStudy.Shared.AbilitySystem.Events;
using Cysharp.Threading.Tasks;
using MessagePipe;
using UnityEngine;
using VContainer.Unity;

namespace CaseStudy.Feature.AbilitySystem.Services
{
    /// <summary>
    /// Orchestrates ability execution flow: trigger, validation, energy, cooldown, and result events.
    /// </summary>
    public sealed class AbilityController : IAbilityController, IStartable, ITickable, IDisposable
    {
        private const float DEFAULT_MINIMUM_LOCK_DURATION_SECONDS = 0.1f;

        private readonly IAbilityFactory _abilityFactory;
        private readonly ICooldownService _cooldownService;
        private readonly IEnergyService _energyService;
        private readonly ISubscriber<AbilityTriggerRequestedEvent> _triggerSubscriber;
        private readonly IPublisher<AbilityTriggeredEvent> _triggeredPublisher;
        private readonly IPublisher<AbilityExecutionFailedEvent> _executionFailedPublisher;

        private readonly Dictionary<string, IAbility> _abilityBySlotKey = new(StringComparer.Ordinal);
        private readonly Dictionary<string, AbilityDataSO> _abilityDataByKey = new(StringComparer.Ordinal);
        private readonly List<IAbility> _configuredAbilities = new(4);

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

        public void Tick()
        {
            float deltaTime = UnityEngine.Time.deltaTime;
            int count = _configuredAbilities.Count;

            for (int i = 0; i < count; i++)
            {
                _configuredAbilities[i].Tick(deltaTime);
            }
        }

        public void Dispose()
        {
            _triggerSubscription?.Dispose();
            _triggerSubscription = null;
        }

        public void Configure(IReadOnlyDictionary<string, AbilityDataSO> loadout, AbilityContext context)
        {
            if (loadout == null)
            {
                throw new ArgumentNullException(nameof(loadout));
            }

            _context = context ?? throw new ArgumentNullException(nameof(context));

            _abilityBySlotKey.Clear();
            _abilityDataByKey.Clear();
            _configuredAbilities.Clear();

            foreach (KeyValuePair<string, AbilityDataSO> pair in loadout)
            {
                string slotKey = NormalizeKey(pair.Key);
                AbilityDataSO data = pair.Value;

                if (string.IsNullOrWhiteSpace(slotKey) || data == null)
                {
                    continue;
                }

                if (!_abilityFactory.TryCreate(data, out IAbility ability))
                {
                    continue;
                }

                if (_abilityBySlotKey.ContainsKey(slotKey))
                {
                    Debug.LogWarning($"AbilityController: duplicate slot key '{slotKey}' detected. Last ability wins.");
                }

                ability.Initialize(_context, data);
                _abilityBySlotKey[slotKey] = ability;
                _abilityDataByKey[data.AbilityKey] = data;
                _configuredAbilities.Add(ability);
            }
        }

        public bool TryTrigger(string slotKey)
        {
            slotKey = NormalizeKey(slotKey);
            if (string.IsNullOrWhiteSpace(slotKey))
            {
                return false;
            }

            if (!_abilityBySlotKey.TryGetValue(slotKey, out IAbility ability))
            {
                return false;
            }

            string abilityKey = ability.AbilityKey;

            if (!_cooldownService.IsReady(abilityKey))
            {
                _executionFailedPublisher.Publish(new AbilityExecutionFailedEvent(abilityKey, AbilityFailureReason.CooldownActive));
                return false;
            }

            if (!_abilityDataByKey.TryGetValue(abilityKey, out AbilityDataSO data))
            {
                _executionFailedPublisher.Publish(new AbilityExecutionFailedEvent(abilityKey, AbilityFailureReason.InvalidConfiguration));
                return false;
            }

            if (!ability.CanExecute())
            {
                _executionFailedPublisher.Publish(new AbilityExecutionFailedEvent(abilityKey, AbilityFailureReason.InvalidConfiguration));
                return false;
            }

            if (!_energyService.TryConsume(data.EnergyCost))
            {
                _executionFailedPublisher.Publish(new AbilityExecutionFailedEvent(abilityKey, AbilityFailureReason.NotEnoughEnergy));
                return false;
            }

            ExecuteAbilityAsync(ability, data).Forget();
            return true;
        }

        public float GetCooldownRemaining(string abilityKey)
        {
            return _cooldownService.GetRemaining(abilityKey);
        }

        private void OnTriggerRequested(AbilityTriggerRequestedEvent evt)
        {
            TryTrigger(evt.SlotKey);
        }

        private async UniTaskVoid ExecuteAbilityAsync(IAbility ability, AbilityDataSO data)
        {
            bool locomotionLockPushed = false;
            bool shouldLockLocomotion = ShouldLockLocomotion(data);
            float lockStartedAt = 0f;

            try
            {
                if (shouldLockLocomotion && _context?.LocomotionLockService != null)
                {
                    _context.LocomotionLockService.PushLock();
                    locomotionLockPushed = true;
                    lockStartedAt = Time.time;
                }

                await ability.ExecuteAsync(CancellationToken.None);

                _cooldownService.StartCooldown(ability.AbilityKey, data.CooldownSeconds);
                _triggeredPublisher.Publish(new AbilityTriggeredEvent(ability.AbilityKey));

                if (locomotionLockPushed)
                {
                    await HoldMinimumLockAsync(data, lockStartedAt);
                }
            }
            catch
            {
                _energyService.Restore(data.EnergyCost);
                _executionFailedPublisher.Publish(new AbilityExecutionFailedEvent(ability.AbilityKey, AbilityFailureReason.InvalidConfiguration));
            }
            finally
            {
                if (locomotionLockPushed && _context?.LocomotionLockService != null)
                {
                    _context.LocomotionLockService.PopLock();
                }
            }
        }

        private static async UniTask HoldMinimumLockAsync(AbilityDataSO data, float lockStartedAt)
        {
            float configuredMinimumSeconds = data?.MinimumMovementLockDurationSeconds ?? 0f;
            if (configuredMinimumSeconds <= 0f)
            {
                configuredMinimumSeconds = DEFAULT_MINIMUM_LOCK_DURATION_SECONDS;
            }

            float minimumLockDurationSeconds = Mathf.Max(configuredMinimumSeconds, Time.fixedDeltaTime);
            float elapsedSeconds = Mathf.Max(0f, Time.time - lockStartedAt);
            float remainingSeconds = minimumLockDurationSeconds - elapsedSeconds;
            if (remainingSeconds <= 0f)
            {
                return;
            }

            int delayMilliseconds = Mathf.CeilToInt(remainingSeconds * 1000f);
            if (delayMilliseconds <= 0)
            {
                await UniTask.WaitForFixedUpdate();
                return;
            }

            await UniTask.Delay(delayMilliseconds, DelayType.DeltaTime, PlayerLoopTiming.Update, CancellationToken.None);
        }

        private static bool ShouldLockLocomotion(AbilityDataSO data)
        {
            return data != null && data.ShouldLockLocomotion;
        }

        private static string NormalizeKey(string key)
        {
            return string.IsNullOrWhiteSpace(key) ? string.Empty : key.Trim();
        }
    }
}

