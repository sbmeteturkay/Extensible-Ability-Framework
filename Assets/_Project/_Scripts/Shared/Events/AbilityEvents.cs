using CaseStudy.Feature.AbilitySystem.Domain;

namespace CaseStudy.Shared.AbilitySystem.Events
{
    public readonly struct AbilityTriggerRequestedEvent
    {
        public AbilityTriggerRequestedEvent(AbilitySlot slot)
        {
            Slot = slot;
        }

        public AbilitySlot Slot { get; }
    }

    public readonly struct AbilityTriggeredEvent
    {
        public AbilityTriggeredEvent(AbilityId abilityId)
        {
            AbilityId = abilityId;
        }

        public AbilityId AbilityId { get; }
    }

    public readonly struct AbilityExecutionFailedEvent
    {
        public AbilityExecutionFailedEvent(AbilityId abilityId, AbilityFailureReason reason)
        {
            AbilityId = abilityId;
            Reason = reason;
        }

        public AbilityId AbilityId { get; }

        public AbilityFailureReason Reason { get; }
    }

    public readonly struct AbilityCooldownStartedEvent
    {
        public AbilityCooldownStartedEvent(AbilityId abilityId, float durationSeconds)
        {
            AbilityId = abilityId;
            DurationSeconds = durationSeconds;
        }

        public AbilityId AbilityId { get; }

        public float DurationSeconds { get; }
    }

    public readonly struct AbilityCooldownUpdatedEvent
    {
        public AbilityCooldownUpdatedEvent(AbilityId abilityId, float remainingSeconds, float normalizedRemaining)
        {
            AbilityId = abilityId;
            RemainingSeconds = remainingSeconds;
            NormalizedRemaining = normalizedRemaining;
        }

        public AbilityId AbilityId { get; }

        public float RemainingSeconds { get; }

        public float NormalizedRemaining { get; }
    }

    public readonly struct AbilityCooldownCompletedEvent
    {
        public AbilityCooldownCompletedEvent(AbilityId abilityId)
        {
            AbilityId = abilityId;
        }

        public AbilityId AbilityId { get; }
    }

    public readonly struct AbilityEnergyChangedEvent
    {
        public AbilityEnergyChangedEvent(float currentEnergy, float maxEnergy)
        {
            CurrentEnergy = currentEnergy;
            MaxEnergy = maxEnergy;
        }

        public float CurrentEnergy { get; }

        public float MaxEnergy { get; }
    }

    public enum AbilityFailureReason
    {
        None = 0,
        CooldownActive = 1,
        NotEnoughEnergy = 2,
        InvalidConfiguration = 3
    }
}