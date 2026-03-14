using CaseStudy.Feature.AbilitySystem.Abilities;
using CaseStudy.Feature.AbilitySystem.Contracts;
using CaseStudy.Feature.AbilitySystem.Data;

namespace CaseStudy.Feature.AbilitySystem.Services
{
    /// <summary>
    /// Creates runtime ability instances from executor-based AbilityDataSO definitions.
    /// </summary>
    public sealed class AbilityFactory : IAbilityFactory
    {
        public bool TryCreate(AbilityDataSO data, out IAbility ability)
        {
            ability = null;

            if (data == null || data.Executor == null)
            {
                return false;
            }

            ability = new ExecutorAbility(data.Executor);
            return true;
        }
    }
}
