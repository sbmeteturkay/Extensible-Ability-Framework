using CaseStudy.Feature.AbilitySystem.Data;
using UnityEngine;

namespace CaseStudy.Feature.AbilitySystem.Domain
{
    /// <summary>
    /// Mutable runtime options built from AbilityData and optionally changed by overrides before execution.
    /// </summary>
    public struct AbilityExecutionOptions
    {
        public float EnergyCost;
        public float CooldownSeconds;
        public bool ShouldLockLocomotion;
        public float MinimumMovementLockDurationSeconds;

        public static AbilityExecutionOptions FromData(AbilityDataSO data)
        {
            if (data == null)
            {
                return default;
            }

            return new AbilityExecutionOptions
            {
                EnergyCost = data.EnergyCost,
                CooldownSeconds = data.CooldownSeconds,
                ShouldLockLocomotion = data.ShouldLockLocomotion,
                MinimumMovementLockDurationSeconds = data.MinimumMovementLockDurationSeconds
            };
        }

        public void Sanitize()
        {
            EnergyCost = Mathf.Max(0f, EnergyCost);
            CooldownSeconds = Mathf.Max(0f, CooldownSeconds);
            MinimumMovementLockDurationSeconds = Mathf.Max(0f, MinimumMovementLockDurationSeconds);
        }
    }
}
