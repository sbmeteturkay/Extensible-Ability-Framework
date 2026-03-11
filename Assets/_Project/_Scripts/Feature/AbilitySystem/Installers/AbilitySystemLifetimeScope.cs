using CaseStudy.Feature.AbilitySystem.Contracts;
using CaseStudy.Feature.AbilitySystem.Input;
using CaseStudy.Feature.AbilitySystem.Runtime;
using CaseStudy.Feature.AbilitySystem.Services;
using CaseStudy.Feature.AbilitySystem.UI;
using CaseStudy.Shared.AbilitySystem.Events;
using MessagePipe;
using VContainer;
using VContainer.Unity;

namespace CaseStudy.Feature.AbilitySystem.Installers
{
    /// <summary>
    /// Feature-local DI scope for the ability system.
    /// Keeps ability services independent from core template scope.
    /// </summary>
    public sealed class AbilitySystemLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            MessagePipeOptions messagePipeOptions = builder.RegisterMessagePipe();
            builder.RegisterMessageBroker<AbilityTriggerRequestedEvent>(messagePipeOptions);
            builder.RegisterMessageBroker<AbilityTriggeredEvent>(messagePipeOptions);
            builder.RegisterMessageBroker<AbilityExecutionFailedEvent>(messagePipeOptions);
            builder.RegisterMessageBroker<AbilityCooldownStartedEvent>(messagePipeOptions);
            builder.RegisterMessageBroker<AbilityCooldownUpdatedEvent>(messagePipeOptions);
            builder.RegisterMessageBroker<AbilityCooldownCompletedEvent>(messagePipeOptions);
            builder.RegisterMessageBroker<AbilityEnergyChangedEvent>(messagePipeOptions);
            builder.RegisterMessageBroker<AbilityLoadoutSlotAssignedEvent>(messagePipeOptions);

            builder.RegisterComponentInHierarchy<AbilityRuntimeBootstrap>();
            builder.RegisterComponentInHierarchy<AbilityInputGateway>();
            builder.RegisterComponentInHierarchy<AbilityHudPresenter>();

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