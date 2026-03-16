using CaseStudy.Feature.AbilitySystem.Contracts;
using CaseStudy.Feature.AbilitySystem.Input;
using CaseStudy.Feature.AbilitySystem.Runtime;
using CaseStudy.Feature.AbilitySystem.Services;
using CaseStudy.Shared.Vfx.Interfaces;
using CaseStudy.Shared.Vfx.Services;
using UnityEngine;
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
            if (!TryGetComponent(out AbilityRuntimeBootstrap runtimeBootstrap))
            {
                Debug.LogWarning("AbilityPlayerLifetimeScope: AbilityRuntimeBootstrap is missing on scope object.", this);
                return;
            }

            if (!TryGetComponent(out AbilityInputGateway abilityInputGateway))
            {
                Debug.LogWarning("AbilityPlayerLifetimeScope: AbilityInputGateway is missing on scope object.", this);
                return;
            }

            builder.RegisterComponent(abilityInputGateway);
            builder.Register<IAbilityInputGate>(resolver => resolver.Resolve<AbilityInputGateway>(), Lifetime.Singleton);
            builder.RegisterComponent(runtimeBootstrap);

            builder.Register<CooldownService>(Lifetime.Singleton);
            builder.Register<ICooldownService>(resolver => resolver.Resolve<CooldownService>(), Lifetime.Singleton);
            builder.RegisterEntryPoint<CooldownService>();

            builder.Register<EnergyService>(Lifetime.Singleton);
            builder.Register<IEnergyService>(resolver => resolver.Resolve<EnergyService>(), Lifetime.Singleton);
            builder.RegisterEntryPoint<EnergyService>();

            builder.Register<PooledVfxService>(Lifetime.Singleton);
            builder.Register<IPooledVfxService>(resolver => resolver.Resolve<PooledVfxService>(), Lifetime.Singleton);

            builder.Register<IAbilityFactory, AbilityFactory>(Lifetime.Singleton);
            builder.Register<AbilityController>(Lifetime.Singleton);
            builder.Register<IAbilityController>(resolver => resolver.Resolve<AbilityController>(), Lifetime.Singleton);
            builder.RegisterEntryPoint<AbilityController>();
        }
    }
}
