using CaseStudy.Core.PlayerControl.Contracts;
using CaseStudy.Core.PlayerControl.Services;
using CaseStudy.Shared.AbilitySystem.Events.Domain;
using CaseStudy.Shared.AbilitySystem.Events.Presentation;
using MessagePipe;
using VContainer;
using VContainer.Unity;

namespace CaseStudy.Core.Installers
{
    /// <summary>
    /// Shared gameplay parent scope.
    /// Registers cross-feature event brokers so child feature scopes can communicate.
    /// </summary>
    public sealed class GamePlayLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            MessagePipeOptions messagePipeOptions = builder.RegisterMessagePipe();

            builder.Register<PlayerSwitchService>(Lifetime.Singleton);
            builder.Register<IPlayerSwitchService>(resolver => resolver.Resolve<PlayerSwitchService>(), Lifetime.Singleton);

            builder.RegisterMessageBroker<AbilityTriggerRequestedEvent>(messagePipeOptions);
            builder.RegisterMessageBroker<AbilityTriggeredEvent>(messagePipeOptions);
            builder.RegisterMessageBroker<AbilityExecutionFailedEvent>(messagePipeOptions);
            builder.RegisterMessageBroker<AbilityExecutionDiagnosticEvent>(messagePipeOptions);
            builder.RegisterMessageBroker<AbilityCooldownStartedEvent>(messagePipeOptions);
            builder.RegisterMessageBroker<AbilityCooldownUpdatedEvent>(messagePipeOptions);
            builder.RegisterMessageBroker<AbilityCooldownCompletedEvent>(messagePipeOptions);
            builder.RegisterMessageBroker<AbilityEnergyChangedEvent>(messagePipeOptions);
            builder.RegisterMessageBroker<AbilityLoadoutSlotAssignedEvent>(messagePipeOptions);
        }
    }
}
