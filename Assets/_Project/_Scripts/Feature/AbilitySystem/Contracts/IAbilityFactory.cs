using CaseStudy.Feature.AbilitySystem.Data;

namespace CaseStudy.Feature.AbilitySystem.Contracts
{
    public interface IAbilityFactory
    {
        bool TryCreate(AbilityDataSO data, out IAbility ability);
    }
}
