using System.Collections.Generic;
using CaseStudy.Feature.AbilitySystem.Data;
using CaseStudy.Feature.AbilitySystem.Domain;

namespace CaseStudy.Feature.AbilitySystem.Contracts
{
    public interface IAbilityController
    {
        void Configure(IReadOnlyDictionary<int, AbilityDataSO> loadout, AbilityContext context);

        bool TryTrigger(int slotIndex);

        float GetCooldownRemaining(string abilityKey);
    }
}
