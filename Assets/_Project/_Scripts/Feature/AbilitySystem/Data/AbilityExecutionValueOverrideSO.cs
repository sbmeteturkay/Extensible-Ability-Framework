using CaseStudy.Feature.AbilitySystem.Domain;
using CaseStudy.Shared.AbilitySystem.Events;
using UnityEngine;

namespace CaseStudy.Feature.AbilitySystem.Data
{
    [CreateAssetMenu(fileName = "SO_Override_ExecutionValues", menuName = "Ability/Overrides/Execution Values")]
    public sealed class AbilityExecutionValueOverrideSO : AbilityOverrideSO
    {
        [Header("Energy")]
        [SerializeField] private bool _overrideEnergyCost;
        [SerializeField, Min(0f)] private float _energyCost;

        [Header("Cooldown")]
        [SerializeField] private bool _overrideCooldownSeconds;
        [SerializeField, Min(0f)] private float _cooldownSeconds = 1f;

        [Header("Movement Lock")]
        [SerializeField] private bool _overrideMovementLock;
        [SerializeField] private bool _shouldLockLocomotion;
        [SerializeField] private bool _overrideMinimumLockDuration;
        [SerializeField, Min(0f)] private float _minimumLockDurationSeconds;

        [Header("Optional Blocking")]
        [SerializeField] private bool _blockTrigger;
        [SerializeField] private AbilityFailureReason _blockReason = AbilityFailureReason.InvalidConfiguration;

        public override bool TryApplyBeforeTrigger(
            AbilityContext context,
            AbilityDataSO data,
            ref AbilityExecutionOptions options,
            out AbilityFailureReason failureReason)
        {
            if (_blockTrigger)
            {
                failureReason = _blockReason == AbilityFailureReason.None
                    ? AbilityFailureReason.InvalidConfiguration
                    : _blockReason;
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

            if (_overrideMovementLock)
            {
                options.ShouldLockLocomotion = _shouldLockLocomotion;
            }

            if (_overrideMinimumLockDuration)
            {
                options.MinimumMovementLockDurationSeconds = _minimumLockDurationSeconds;
            }

            failureReason = AbilityFailureReason.None;
            return true;
        }
    }
}
