using CaseStudy.Feature.AbilitySystem.Contracts;
using CaseStudy.Feature.AbilitySystem.Runtime;
using CaseStudy.Feature.AbilitySystem.Services;
using VContainer;
using VContainer.Unity;

namespace CaseStudy.Feature.AbilitySystem.Installers
{
    /// <summary>
    /// Player-side scope for ability runtime and domain services.
    /// Place this under player prefab feature hierarchy.
    /// </summary>
    public sealed class AbilityPlayerLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<AbilityRuntimeBootstrap>();

            builder.Register<CooldownService>(Lifetime.Singleton);
            builder.Register<ICooldownService>(resolver => resolver.Resolve<CooldownService>(), Lifetime.Singleton);
            builder.RegisterEntryPoint<CooldownService>();

            builder.Register<EnergyService>(Lifetime.Singleton);
            builder.Register<IEnergyService>(resolver => resolver.Resolve<EnergyService>(), Lifetime.Singleton);
            builder.RegisterEntryPoint<EnergyService>();

            builder.Register<IAbilityFactory, AbilityFactory>(Lifetime.Singleton);
            builder.Register<AbilityController>(Lifetime.Singleton);
            builder.Register<IAbilityController>(resolver => resolver.Resolve<AbilityController>(), Lifetime.Singleton);
            builder.RegisterEntryPoint<AbilityController>();
            builder.RegisterEntryPoint<AbilityFactoryBootstrapper>();
        }
    }
}
