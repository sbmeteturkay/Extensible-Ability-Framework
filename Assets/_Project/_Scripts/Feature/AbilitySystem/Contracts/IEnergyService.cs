namespace CaseStudy.Feature.AbilitySystem.Contracts
{
    public interface IEnergyService
    {
        float CurrentEnergy { get; }

        float MaxEnergy { get; }

        bool HasEnough(float cost);

        bool TryConsume(float cost);

        void Restore(float amount);
    }
}