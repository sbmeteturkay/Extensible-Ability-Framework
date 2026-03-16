using CaseStudy.Feature.AbilitySystem.Data;

namespace CaseStudy.Feature.AbilitySystem.Contracts
{
    public enum AbilityVisualKind
    {
        Hit = 0,
        Aoe = 1
    }

    public readonly struct AbilityVisualCommand
    {
        public AbilityVisualCommand(AbilityVisualKind kind, HitVisualProfileSO profile, float durationSeconds)
        {
            Kind = kind;
            Profile = profile;
            DurationSeconds = durationSeconds;
        }

        public AbilityVisualKind Kind { get; }
        public HitVisualProfileSO Profile { get; }
        public float DurationSeconds { get; }
    }
}
