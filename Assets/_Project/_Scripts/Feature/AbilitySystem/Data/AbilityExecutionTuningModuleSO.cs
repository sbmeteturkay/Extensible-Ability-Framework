using CaseStudy.Feature.AbilitySystem.Domain;
using CaseStudy.Shared.AbilitySystem.Events;
using UnityEngine;

namespace CaseStudy.Feature.AbilitySystem.Data
{
    [CreateAssetMenu(fileName = "SO_Module_ExecutionTuning", menuName = "Ability/Modules/Execution Tuning")]
    public sealed class AbilityExecutionTuningModuleSO : AbilityOptionalModuleSO
    {
        [Header("Energy")]
        [SerializeField] private bool _overrideEnergyCost;
        [SerializeField, Min(0f)] private float _energyCost;

        [Header("Cooldown")]
        [SerializeField] private bool _overrideCooldownSeconds;
        [SerializeField, Min(0f)] private float _cooldownSeconds = 1f;

        [Header("Movement Lock")]
        [SerializeField] private bool _overrideShouldLockLocomotion;
        [SerializeField] private bool _shouldLockLocomotion;
        [SerializeField] private bool _overrideMinimumLockDuration;
        [SerializeField, Min(0f)] private float _minimumLockDurationSeconds;

        [Header("Optional Blocking")]
        [SerializeField] private bool _blockTrigger;
        [SerializeField] private AbilityFailureReason _blockReason = AbilityFailureReason.InvalidConfiguration;
        [SerializeField] private string _blockMessage = "Blocked by AbilityExecutionTuningModuleSO.";

        public override bool TryApplyBeforeTrigger(
            AbilityContext context,
            AbilityDataSO data,
            ref AbilityExecutionOptions options,
            out AbilityFailureReason failureReason,
            out string failureMessage)
        {
            if (_blockTrigger)
            {
                failureReason = _blockReason == AbilityFailureReason.None
                    ? AbilityFailureReason.InvalidConfiguration
                    : _blockReason;
                failureMessage = string.IsNullOrWhiteSpace(_blockMessage)
                    ? "Blocked by AbilityExecutionTuningModuleSO."
                    : _blockMessage;
                return false;
            }

            if (_overrideEnergyCost)
            {
                options.EnergyCost = _energyCost;
            }

            if (_overrideCooldownSeconds)
            {
                options.CooldownSeconds = _cooldownSeconds;
            }

            if (_overrideShouldLockLocomotion)
            {
                options.ShouldLockLocomotion = _shouldLockLocomotion;
            }

            if (_overrideMinimumLockDuration)
            {
                options.MinimumMovementLockDurationSeconds = _minimumLockDurationSeconds;
            }

            failureReason = AbilityFailureReason.None;
            failureMessage = string.Empty;
            return true;
        }
    }
}
