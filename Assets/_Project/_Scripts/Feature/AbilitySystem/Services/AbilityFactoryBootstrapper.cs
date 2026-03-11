using CaseStudy.Feature.AbilitySystem.Abilities;
using CaseStudy.Feature.AbilitySystem.Contracts;
using CaseStudy.Feature.AbilitySystem.Data;
using VContainer.Unity;

namespace CaseStudy.Feature.AbilitySystem.Services
{
    /// <summary>
    /// Registers concrete ability creators into the ability factory at startup.
    /// </summary>
    public sealed class AbilityFactoryBootstrapper : IStartable
    {
        private readonly IAbilityFactory _abilityFactory;

        public AbilityFactoryBootstrapper(IAbilityFactory abilityFactory)
        {
            _abilityFactory = abilityFactory;
        }

        public void Start()
        {
            _abilityFactory.Register<ProjectileAbilityDataSO>(_ => new ProjectileAbility());
            _abilityFactory.Register<AoeAbilityDataSO>(_ => new AoeAbility());
            _abilityFactory.Register<DashAbilityDataSO>(_ => new DashAbility());
        }
    }
}