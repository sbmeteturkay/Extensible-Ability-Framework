using System.Collections.Generic;
using CaseStudy.Feature.AbilitySystem.Data;
using CaseStudy.Feature.AbilitySystem.Domain;

namespace CaseStudy.Feature.AbilitySystem.Contracts
{
    public interface IAbilityController
    {
        void Configure(IReadOnlyDictionary<AbilitySlot, AbilityDataSO> loadout, AbilityContext context);

        bool TryTrigger(AbilitySlot slot);

        float GetCooldownRemaining(AbilityId abilityId);
    }
}