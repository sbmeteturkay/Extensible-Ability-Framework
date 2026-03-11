using System;
using System.Collections.Generic;
using CaseStudy.Feature.AbilitySystem.Contracts;
using CaseStudy.Feature.AbilitySystem.Data;

namespace CaseStudy.Feature.AbilitySystem.Services
{
    /// <summary>
    /// Creates runtime ability instances from AbilityDataSO types via registered creators.
    /// </summary>
    public sealed class AbilityFactory : IAbilityFactory
    {
        private readonly Dictionary<Type, Func<AbilityDataSO, IAbility>> _creators = new();

        public void Register<TAbilityData>(Func<TAbilityData, IAbility> creator)
            where TAbilityData : AbilityDataSO
        {
            if (creator == null)
            {
                throw new ArgumentNullException(nameof(creator));
            }

            _creators[typeof(TAbilityData)] = data => creator((TAbilityData)data);
        }

        public bool TryCreate(AbilityDataSO data, out IAbility ability)
        {
            ability = null;

            if (data == null)
            {
                return false;
            }

            if (!_creators.TryGetValue(data.GetType(), out Func<AbilityDataSO, IAbility> creator))
            {
                return false;
            }

            ability = creator(data);
            return ability != null;
        }
    }
}