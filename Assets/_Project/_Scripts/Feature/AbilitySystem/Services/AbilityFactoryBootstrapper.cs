using CaseStudy.Feature.AbilitySystem.Abilities;
using CaseStudy.Feature.AbilitySystem.Contracts;
using CaseStudy.Feature.AbilitySystem.Data;
using CaseStudy.Shared.Vfx.Interfaces;
using VContainer.Unity;

namespace CaseStudy.Feature.AbilitySystem.Services
{
    /// <summary>
    /// Registers concrete ability creators into the ability factory at startup.
    /// </summary>
    public sealed class AbilityFactoryBootstrapper : IStartable
    {
        private readonly IAbilityFactory _abilityFactory;
        private readonly IPooledVfxService _pooledVfxService;

        public AbilityFactoryBootstrapper(IAbilityFactory abilityFactory, IPooledVfxService pooledVfxService)
        {
            _abilityFactory = abilityFactory;
            _pooledVfxService = pooledVfxService;
        }

        public void Start()
        {
            _abilityFactory.Register<ProjectileAbilityDataSO>(_ => new ProjectileAbility(_pooledVfxService));
            _abilityFactory.Register<AoeAbilityDataSO>(_ => new AoeAbility(_pooledVfxService));
            _abilityFactory.Register<DashAbilityDataSO>(_ => new DashAbility());
        }
    }
}
