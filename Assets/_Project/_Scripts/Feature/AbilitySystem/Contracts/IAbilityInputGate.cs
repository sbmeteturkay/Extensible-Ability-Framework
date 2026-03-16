namespace CaseStudy.Feature.AbilitySystem.Contracts
{
    /// <summary>
    /// Provides the player-local input gate state used by ability trigger flow.
    /// </summary>
    public interface IAbilityInputGate
    {
        bool IsInputGateOpen { get; }
    }
}
