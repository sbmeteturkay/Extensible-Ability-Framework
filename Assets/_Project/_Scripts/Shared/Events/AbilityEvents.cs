using UnityEngine;

namespace CaseStudy.Shared.AbilitySystem.Events
{
    public readonly struct AbilityTriggerRequestedEvent
    {
        public AbilityTriggerRequestedEvent(string slotKey)
        {
            SlotKey = slotKey;
        }

        public string SlotKey { get; }
    }

    public readonly struct AbilityTriggeredEvent
    {
        public AbilityTriggeredEvent(string abilityKey)
        {
            AbilityKey = abilityKey;
        }

        public string AbilityKey { get; }
    }

    public readonly struct AbilityExecutionFailedEvent
    {
        public AbilityExecutionFailedEvent(string abilityKey, AbilityFailureReason reason)
        {
            AbilityKey = abilityKey;
            Reason = reason;
        }

        public string AbilityKey { get; }

        public AbilityFailureReason Reason { get; }
    }

    public readonly struct AbilityExecutionDiagnosticEvent
    {
        public AbilityExecutionDiagnosticEvent(
            string slotKey,
            string abilityKey,
            AbilityFailureReason reason,
            string stage,
            string source,
            string message)
        {
            SlotKey = slotKey;
            AbilityKey = abilityKey;
            Reason = reason;
            Stage = stage;
            Source = source;
            Message = message;
        }

        public string SlotKey { get; }

        public string AbilityKey { get; }

        public AbilityFailureReason Reason { get; }

        public string Stage { get; }

        public string Source { get; }

        public string Message { get; }
    }

    public readonly struct AbilityCooldownStartedEvent
    {
        public AbilityCooldownStartedEvent(string abilityKey, float durationSeconds)
        {
            AbilityKey = abilityKey;
            DurationSeconds = durationSeconds;
        }

        public string AbilityKey { get; }

        public float DurationSeconds { get; }
    }

    public readonly struct AbilityCooldownUpdatedEvent
    {
        public AbilityCooldownUpdatedEvent(string abilityKey, float remainingSeconds, float normalizedRemaining)
        {
            AbilityKey = abilityKey;
            RemainingSeconds = remainingSeconds;
            NormalizedRemaining = normalizedRemaining;
        }

        public string AbilityKey { get; }

        public float RemainingSeconds { get; }

        public float NormalizedRemaining { get; }
    }

    public readonly struct AbilityCooldownCompletedEvent
    {
        public AbilityCooldownCompletedEvent(string abilityKey)
        {
            AbilityKey = abilityKey;
        }

        public string AbilityKey { get; }
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

    public readonly struct AbilityLoadoutSlotAssignedEvent
    {
        public AbilityLoadoutSlotAssignedEvent(string slotKey, string abilityKey, Sprite icon)
        {
            SlotKey = slotKey;
            AbilityKey = abilityKey;
            Icon = icon;
        }

        public string SlotKey { get; }

        public string AbilityKey { get; }

        public Sprite Icon { get; }
    }

    public enum AbilityFailureReason
    {
        None = 0,
        CooldownActive = 1,
        NotEnoughEnergy = 2,
        InvalidConfiguration = 3
    }
}
