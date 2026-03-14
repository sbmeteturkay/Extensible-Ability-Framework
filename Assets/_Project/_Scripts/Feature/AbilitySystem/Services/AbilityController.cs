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
        private readonly IPublisher<AbilityExecutionDiagnosticEvent> _executionDiagnosticPublisher;

        private readonly Dictionary<string, IAbility> _abilityBySlotKey = new(StringComparer.Ordinal);
        private readonly Dictionary<string, AbilityDataSO> _abilityDataByKey = new(StringComparer.Ordinal);
        private readonly Dictionary<string, AbilityOverrideSO[]> _overridesByAbilityKey = new(StringComparer.Ordinal);
        private readonly List<IAbility> _configuredAbilities = new(4);

        private AbilityContext _context;
        private IDisposable _triggerSubscription;

        public AbilityController(
            IAbilityFactory abilityFactory,
            ICooldownService cooldownService,
            IEnergyService energyService,
            ISubscriber<AbilityTriggerRequestedEvent> triggerSubscriber,
            IPublisher<AbilityTriggeredEvent> triggeredPublisher,
            IPublisher<AbilityExecutionFailedEvent> executionFailedPublisher,
            IPublisher<AbilityExecutionDiagnosticEvent> executionDiagnosticPublisher)
        {
            _abilityFactory = abilityFactory;
            _cooldownService = cooldownService;
            _energyService = energyService;
            _triggerSubscriber = triggerSubscriber;
            _triggeredPublisher = triggeredPublisher;
            _executionFailedPublisher = executionFailedPublisher;
            _executionDiagnosticPublisher = executionDiagnosticPublisher;
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
            _overridesByAbilityKey.Clear();
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
                _overridesByAbilityKey[data.AbilityKey] = BuildOverrideArray(data.Overrides);
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
                PublishFailure(
                    slotKey,
                    string.Empty,
                    AbilityFailureReason.InvalidConfiguration,
                    "ResolveSlot",
                    nameof(AbilityController),
                    "No ability configured for slot.");
                return false;
            }

            string abilityKey = ability.AbilityKey;

            if (!_abilityDataByKey.TryGetValue(abilityKey, out AbilityDataSO data))
            {
                PublishFailure(
                    slotKey,
                    abilityKey,
                    AbilityFailureReason.InvalidConfiguration,
                    "ResolveData",
                    nameof(AbilityController),
                    "Ability data is missing from controller mapping.");
                return false;
            }

            AbilityExecutionOptions executionOptions = AbilityExecutionOptions.FromData(data);
            if (!TryApplyBeforeTriggerOverrides(
                    data,
                    ref executionOptions,
                    out AbilityFailureReason overrideFailureReason,
                    out string overrideFailureSource,
                    out string overrideFailureMessage))
            {
                PublishFailure(
                    slotKey,
                    abilityKey,
                    overrideFailureReason,
                    "BeforeTriggerOverrides",
                    overrideFailureSource,
                    overrideFailureMessage);
                return false;
            }

            if (!_cooldownService.IsReady(abilityKey))
            {
                PublishFailure(
                    slotKey,
                    abilityKey,
                    AbilityFailureReason.CooldownActive,
                    "CooldownCheck",
                    nameof(ICooldownService),
                    "Cooldown is still active.");
                return false;
            }

            if (!ability.CanExecute())
            {
                PublishFailure(
                    slotKey,
                    abilityKey,
                    AbilityFailureReason.InvalidConfiguration,
                    "CanExecute",
                    ability.GetType().Name,
                    "Ability.CanExecute returned false.");
                return false;
            }

            if (!_energyService.TryConsume(executionOptions.EnergyCost))
            {
                PublishFailure(
                    slotKey,
                    abilityKey,
                    AbilityFailureReason.NotEnoughEnergy,
                    "EnergyCheck",
                    nameof(IEnergyService),
                    "Not enough energy.");
                return false;
            }

            ExecuteAbilityAsync(slotKey, ability, data, executionOptions).Forget();
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

        private async UniTaskVoid ExecuteAbilityAsync(string slotKey, IAbility ability, AbilityDataSO data, AbilityExecutionOptions executionOptions)
        {
            bool locomotionLockPushed = false;
            bool shouldLockLocomotion = executionOptions.ShouldLockLocomotion;
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

                _cooldownService.StartCooldown(ability.AbilityKey, executionOptions.CooldownSeconds);
                _triggeredPublisher.Publish(new AbilityTriggeredEvent(ability.AbilityKey,_configuredAbilities.IndexOf(ability)));

                await InvokeAfterExecuteOverridesAsync(data, executionOptions);

                if (locomotionLockPushed)
                {
                    await HoldMinimumLockAsync(executionOptions.MinimumMovementLockDurationSeconds, lockStartedAt);
                }
            }
            catch (Exception exception)
            {
                _energyService.Restore(executionOptions.EnergyCost);

                PublishFailure(
                    slotKey,
                    ability.AbilityKey,
                    AbilityFailureReason.InvalidConfiguration,
                    "ExecuteAsync",
                    ability.GetType().Name,
                    exception.Message);
            }
            finally
            {
                if (locomotionLockPushed && _context?.LocomotionLockService != null)
                {
                    _context.LocomotionLockService.PopLock();
                }
            }
        }

        private bool TryApplyBeforeTriggerOverrides(
            AbilityDataSO data,
            ref AbilityExecutionOptions executionOptions,
            out AbilityFailureReason failureReason,
            out string failureSource,
            out string failureMessage)
        {
            failureReason = AbilityFailureReason.None;
            failureSource = string.Empty;
            failureMessage = string.Empty;

            if (data == null)
            {
                failureReason = AbilityFailureReason.InvalidConfiguration;
                failureSource = nameof(AbilityController);
                failureMessage = "Ability data is null.";
                return false;
            }

            if (!_overridesByAbilityKey.TryGetValue(data.AbilityKey, out AbilityOverrideSO[] overrides)
                || overrides == null
                || overrides.Length == 0)
            {
                executionOptions.Sanitize();
                return true;
            }

            for (int i = 0; i < overrides.Length; i++)
            {
                AbilityOverrideSO abilityOverride = overrides[i];
                if (abilityOverride == null)
                {
                    continue;
                }

                if (!abilityOverride.TryApplyBeforeTrigger(_context, data, ref executionOptions, out AbilityFailureReason overrideFailure))
                {
                    failureReason = overrideFailure == AbilityFailureReason.None
                        ? AbilityFailureReason.InvalidConfiguration
                        : overrideFailure;
                    failureSource = abilityOverride.name;
                    failureMessage = "Override blocked trigger in TryApplyBeforeTrigger.";
                    return false;
                }
            }

            executionOptions.Sanitize();
            return true;
        }

        private async UniTask InvokeAfterExecuteOverridesAsync(AbilityDataSO data, AbilityExecutionOptions executionOptions)
        {
            if (data == null)
            {
                return;
            }

            if (!_overridesByAbilityKey.TryGetValue(data.AbilityKey, out AbilityOverrideSO[] overrides)
                || overrides == null
                || overrides.Length == 0)
            {
                return;
            }

            for (int i = 0; i < overrides.Length; i++)
            {
                AbilityOverrideSO abilityOverride = overrides[i];
                if (abilityOverride == null)
                {
                    continue;
                }

                try
                {
                    await abilityOverride.OnAfterExecuteAsync(_context, data, executionOptions, CancellationToken.None);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"AbilityController: after-execute override failed on '{abilityOverride.name}'. {exception.Message}");
                }
            }
        }

        private void PublishFailure(
            string slotKey,
            string abilityKey,
            AbilityFailureReason reason,
            string stage,
            string source,
            string message)
        {
            _executionFailedPublisher.Publish(new AbilityExecutionFailedEvent(abilityKey, reason));
            _executionDiagnosticPublisher.Publish(new AbilityExecutionDiagnosticEvent(
                slotKey,
                abilityKey,
                reason,
                stage,
                string.IsNullOrWhiteSpace(source) ? nameof(AbilityController) : source,
                string.IsNullOrWhiteSpace(message) ? "No failure message." : message));
        }

        private static async UniTask HoldMinimumLockAsync(float configuredMinimumSeconds, float lockStartedAt)
        {
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

        private static AbilityOverrideSO[] BuildOverrideArray(IReadOnlyList<AbilityOverrideSO> configuredOverrides)
        {
            if (configuredOverrides == null || configuredOverrides.Count == 0)
            {
                return Array.Empty<AbilityOverrideSO>();
            }

            int validCount = 0;
            int sourceCount = configuredOverrides.Count;

            for (int i = 0; i < sourceCount; i++)
            {
                if (configuredOverrides[i] != null)
                {
                    validCount++;
                }
            }

            if (validCount == 0)
            {
                return Array.Empty<AbilityOverrideSO>();
            }

            var output = new AbilityOverrideSO[validCount];
            int outputIndex = 0;

            for (int i = 0; i < sourceCount; i++)
            {
                AbilityOverrideSO candidate = configuredOverrides[i];
                if (candidate == null)
                {
                    continue;
                }

                output[outputIndex] = candidate;
                outputIndex++;
            }

            SortOverridesByOrder(output);
            return output;
        }

        private static void SortOverridesByOrder(AbilityOverrideSO[] overrides)
        {
            for (int i = 1; i < overrides.Length; i++)
            {
                AbilityOverrideSO current = overrides[i];
                int order = current != null ? current.Order : 0;
                int j = i - 1;

                while (j >= 0)
                {
                    AbilityOverrideSO previous = overrides[j];
                    int previousOrder = previous != null ? previous.Order : 0;
                    if (previousOrder <= order)
                    {
                        break;
                    }

                    overrides[j + 1] = previous;
                    j--;
                }

                overrides[j + 1] = current;
            }
        }

        private static string NormalizeKey(string key)
        {
            return string.IsNullOrWhiteSpace(key) ? string.Empty : key.Trim();
        }
    }
}
