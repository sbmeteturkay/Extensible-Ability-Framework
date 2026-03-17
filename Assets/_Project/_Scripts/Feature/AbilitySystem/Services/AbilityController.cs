using System;
using System.Collections.Generic;
using System.Threading;
using CaseStudy.Feature.AbilitySystem.Contracts;
using CaseStudy.Feature.AbilitySystem.Data;
using CaseStudy.Feature.AbilitySystem.Domain;
using CaseStudy.Shared.AbilitySystem.Contracts;
using CaseStudy.Shared.AbilitySystem.Events.Domain;
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
        private readonly IAbilityInputGate _abilityInputGate;
        private readonly ISubscriber<AbilityTriggerRequestedEvent> _triggerSubscriber;
        private readonly IPublisher<AbilityTriggeredEvent> _triggeredPublisher;
        private readonly IPublisher<AbilityExecutionFailedEvent> _executionFailedPublisher;
        private readonly IPublisher<AbilityExecutionDiagnosticEvent> _executionDiagnosticPublisher;

        private readonly Dictionary<int, IAbility> _abilityBySlotIndex = new();
        private readonly Dictionary<string, AbilityDataSO> _abilityDataByKey = new(StringComparer.Ordinal);
        private readonly Dictionary<string, AbilityOptionalModuleSO[]> _modulesByAbilityKey = new(StringComparer.Ordinal);
        private readonly HashSet<string> _executingAbilityKeys = new(StringComparer.Ordinal);
        private readonly List<IAbility> _configuredAbilities = new(4);

        private AbilityContext _context;
        private IDisposable _triggerSubscription;

        public AbilityController(
            IAbilityFactory abilityFactory,
            ICooldownService cooldownService,
            IEnergyService energyService,
            IAbilityInputGate abilityInputGate,
            ISubscriber<AbilityTriggerRequestedEvent> triggerSubscriber,
            IPublisher<AbilityTriggeredEvent> triggeredPublisher,
            IPublisher<AbilityExecutionFailedEvent> executionFailedPublisher,
            IPublisher<AbilityExecutionDiagnosticEvent> executionDiagnosticPublisher)
        {
            _abilityFactory = abilityFactory;
            _cooldownService = cooldownService;
            _energyService = energyService;
            _abilityInputGate = abilityInputGate;
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

        public void Configure(IReadOnlyDictionary<int, AbilityDataSO> loadout, AbilityContext context)
        {
            if (loadout == null)
            {
                throw new ArgumentNullException(nameof(loadout));
            }

            _context = context ?? throw new ArgumentNullException(nameof(context));

            _abilityBySlotIndex.Clear();
            _abilityDataByKey.Clear();
            _modulesByAbilityKey.Clear();
            _executingAbilityKeys.Clear();
            _configuredAbilities.Clear();

            foreach (KeyValuePair<int, AbilityDataSO> pair in loadout)
            {
                int slotIndex = pair.Key;
                AbilityDataSO data = pair.Value;

                if (slotIndex < 0 || data == null)
                {
                    continue;
                }

                if (!_abilityFactory.TryCreate(data, out IAbility ability))
                {
                    continue;
                }

                if (_abilityBySlotIndex.ContainsKey(slotIndex))
                {
                    Debug.LogWarning($"AbilityController: duplicate slot index '{slotIndex}' detected. Last ability wins.");
                }

                ability.Initialize(_context, data);
                _abilityBySlotIndex[slotIndex] = ability;
                _abilityDataByKey[data.AbilityKey] = data;
                _modulesByAbilityKey[data.AbilityKey] = BuildOptionalModuleArray(data.OptionalModules);
                _configuredAbilities.Add(ability);
            }
        }

        public bool TryTrigger(int slotIndex)
        {
            if (slotIndex < 0)
            {
                return false;
            }

            if (!_abilityBySlotIndex.TryGetValue(slotIndex, out IAbility ability))
            {
                PublishFailure(
                    slotIndex,
                    string.Empty,
                    AbilityFailureReason.InvalidConfiguration,
                    "ResolveSlot",
                    nameof(AbilityController),
                    "No ability configured for slot index.");
                return false;
            }

            string abilityKey = ability.AbilityKey;

            if (!_abilityDataByKey.TryGetValue(abilityKey, out AbilityDataSO data))
            {
                PublishFailure(
                    slotIndex,
                    abilityKey,
                    AbilityFailureReason.InvalidConfiguration,
                    "ResolveData",
                    nameof(AbilityController),
                    "Ability data is missing from controller mapping.");
                return false;
            }

            AbilityExecutionOptions executionOptions = AbilityExecutionOptions.FromData(data);

            if (!TryApplyBeforeTriggerModules(
                    data,
                    ref executionOptions,
                    out AbilityFailureReason moduleFailureReason,
                    out string moduleFailureSource,
                    out string moduleFailureMessage))
            {
                PublishFailure(
                    slotIndex,
                    abilityKey,
                    moduleFailureReason,
                    "BeforeTriggerModules",
                    moduleFailureSource,
                    moduleFailureMessage);
                return false;
            }

            if (!_cooldownService.IsReady(abilityKey))
            {
                PublishFailure(
                    slotIndex,
                    abilityKey,
                    AbilityFailureReason.CooldownActive,
                    "CooldownCheck",
                    nameof(ICooldownService),
                    "Cooldown is still active.");
                return false;
            }

            if (_executingAbilityKeys.Contains(abilityKey))
            {
                PublishFailure(
                    slotIndex,
                    abilityKey,
                    AbilityFailureReason.CooldownActive,
                    "ExecutionGate",
                    nameof(AbilityController),
                    "Ability execution is already in progress.");
                return false;
            }

            if (!ability.CanExecute())
            {
                PublishFailure(
                    slotIndex,
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
                    slotIndex,
                    abilityKey,
                    AbilityFailureReason.NotEnoughEnergy,
                    "EnergyCheck",
                    nameof(IEnergyService),
                    "Not enough energy.");
                return false;
            }

            _executingAbilityKeys.Add(abilityKey);
            ExecuteAbilityAsync(slotIndex, ability, data, executionOptions).Forget();
            return true;
        }

        public float GetCooldownRemaining(string abilityKey)
        {
            return _cooldownService.GetRemaining(abilityKey);
        }

        private void OnTriggerRequested(AbilityTriggerRequestedEvent evt)
        {
            if (_abilityInputGate != null && !_abilityInputGate.IsInputGateOpen)
            {
                return;
            }

            TryTrigger(evt.SlotIndex);
        }

        private async UniTaskVoid ExecuteAbilityAsync(int slotIndex, IAbility ability, AbilityDataSO data, AbilityExecutionOptions executionOptions)
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

                await InvokeBeforeExecuteModulesAsync(data, executionOptions);

                _triggeredPublisher.Publish(new AbilityTriggeredEvent(ability.AbilityKey, slotIndex));

                await ability.ExecuteAsync(CancellationToken.None);

                _cooldownService.StartCooldown(ability.AbilityKey, executionOptions.CooldownSeconds);

                await InvokeAfterExecuteModulesAsync(data, executionOptions);

                if (locomotionLockPushed)
                {
                    await HoldMinimumLockAsync(executionOptions.MinimumMovementLockDurationSeconds, lockStartedAt);
                }
            }
            catch (Exception exception)
            {
                _energyService.Restore(executionOptions.EnergyCost);

                PublishFailure(
                    slotIndex,
                    ability.AbilityKey,
                    AbilityFailureReason.InvalidConfiguration,
                    "ExecuteAsync",
                    ability.GetType().Name,
                    exception.Message);
            }
            finally
            {
                _executingAbilityKeys.Remove(ability.AbilityKey);

                if (locomotionLockPushed && _context?.LocomotionLockService != null)
                {
                    _context.LocomotionLockService.PopLock();
                }
            }
        }

        private bool TryApplyBeforeTriggerModules(
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

            if (!_modulesByAbilityKey.TryGetValue(data.AbilityKey, out AbilityOptionalModuleSO[] modules)
                || modules == null
                || modules.Length == 0)
            {
                executionOptions.Sanitize();
                return true;
            }

            for (int i = 0; i < modules.Length; i++)
            {
                AbilityOptionalModuleSO module = modules[i];
                if (module == null)
                {
                    continue;
                }

                try
                {
                    if (!module.TryApplyBeforeTrigger(_context, data, ref executionOptions, out AbilityFailureReason moduleFailure, out string moduleFailureMessage))
                    {
                        failureReason = moduleFailure == AbilityFailureReason.None
                            ? AbilityFailureReason.InvalidConfiguration
                            : moduleFailure;
                        failureSource = module.name;
                        failureMessage = string.IsNullOrWhiteSpace(moduleFailureMessage)
                            ? "Module blocked trigger in TryApplyBeforeTrigger."
                            : moduleFailureMessage;
                        return false;
                    }
                }
                catch (Exception exception)
                {
                    failureReason = AbilityFailureReason.InvalidConfiguration;
                    failureSource = module.name;
                    failureMessage = exception.Message;
                    return false;
                }
            }

            executionOptions.Sanitize();
            return true;
        }

        private async UniTask InvokeBeforeExecuteModulesAsync(AbilityDataSO data, AbilityExecutionOptions executionOptions)
        {
            if (data == null)
            {
                return;
            }

            if (!_modulesByAbilityKey.TryGetValue(data.AbilityKey, out AbilityOptionalModuleSO[] modules)
                || modules == null
                || modules.Length == 0)
            {
                return;
            }

            for (int i = 0; i < modules.Length; i++)
            {
                AbilityOptionalModuleSO module = modules[i];
                if (module == null)
                {
                    continue;
                }

                try
                {
                    await module.OnBeforeExecuteAsync(_context, data, executionOptions, CancellationToken.None);
                }
                catch (Exception exception)
                {
                    throw new InvalidOperationException($"Module '{module.name}' failed in OnBeforeExecuteAsync. {exception.Message}", exception);
                }
            }
        }

        private async UniTask InvokeAfterExecuteModulesAsync(AbilityDataSO data, AbilityExecutionOptions executionOptions)
        {
            if (data == null)
            {
                return;
            }

            if (!_modulesByAbilityKey.TryGetValue(data.AbilityKey, out AbilityOptionalModuleSO[] modules)
                || modules == null
                || modules.Length == 0)
            {
                return;
            }

            for (int i = 0; i < modules.Length; i++)
            {
                AbilityOptionalModuleSO module = modules[i];
                if (module == null)
                {
                    continue;
                }

                try
                {
                    await module.OnAfterExecuteAsync(_context, data, executionOptions, CancellationToken.None);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"AbilityController: after-execute module failed on '{module.name}'. {exception.Message}");
                }
            }
        }

        private void PublishFailure(
            int slotIndex,
            string abilityKey,
            AbilityFailureReason reason,
            string stage,
            string source,
            string message)
        {
            _executionFailedPublisher.Publish(new AbilityExecutionFailedEvent(abilityKey, reason));
            _executionDiagnosticPublisher.Publish(new AbilityExecutionDiagnosticEvent(
                slotIndex,
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

        private static AbilityOptionalModuleSO[] BuildOptionalModuleArray(IReadOnlyList<AbilityOptionalModuleSO> configuredModules)
        {
            if (configuredModules == null || configuredModules.Count == 0)
            {
                return Array.Empty<AbilityOptionalModuleSO>();
            }

            int validCount = 0;
            int sourceCount = configuredModules.Count;

            for (int i = 0; i < sourceCount; i++)
            {
                if (configuredModules[i] != null)
                {
                    validCount++;
                }
            }

            if (validCount == 0)
            {
                return Array.Empty<AbilityOptionalModuleSO>();
            }

            var output = new AbilityOptionalModuleSO[validCount];
            int outputIndex = 0;

            for (int i = 0; i < sourceCount; i++)
            {
                AbilityOptionalModuleSO candidate = configuredModules[i];
                if (candidate == null)
                {
                    continue;
                }

                output[outputIndex] = candidate;
                outputIndex++;
            }

            SortModulesByOrder(output);
            return output;
        }

        private static void SortModulesByOrder(AbilityOptionalModuleSO[] modules)
        {
            for (int i = 1; i < modules.Length; i++)
            {
                AbilityOptionalModuleSO current = modules[i];
                int order = current != null ? current.Order : 0;
                int j = i - 1;

                while (j >= 0)
                {
                    AbilityOptionalModuleSO previous = modules[j];
                    int previousOrder = previous != null ? previous.Order : 0;
                    if (previousOrder <= order)
                    {
                        break;
                    }

                    modules[j + 1] = previous;
                    j--;
                }

                modules[j + 1] = current;
            }
        }
    }
}

