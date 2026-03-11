using System;
using CaseStudy.Feature.AbilitySystem.Data;

namespace CaseStudy.Feature.AbilitySystem.Contracts
{
    public interface IAbilityFactory
    {
        void Register<TAbilityData>(Func<TAbilityData, IAbility> creator)
            where TAbilityData : AbilityDataSO;

        bool TryCreate(AbilityDataSO data, out IAbility ability);
    }
}