using CaseStudy.Feature.AbilitySystem.Contracts;
using CaseStudy.Feature.AbilitySystem.Services;
using CaseStudy.Shared.AbilitySystem.Events;
using MessagePipe;
using SabanCoreTemplate.SceneManagement;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace SabanCoreTemplate
{
    public class GameLifetimeScope : LifetimeScope
    {
        [SerializeField] private LoadingScreen _loadingScreenPrefab;
        [SerializeField] private GameObject _defaultSystemsPrefab;

        protected override void Configure(IContainerBuilder builder)
        {
            LoadingScreen loadingScreen = Instantiate(_loadingScreenPrefab);
            builder.RegisterInstance(loadingScreen);

            builder.Register<ISceneLoader, SceneLoader>(Lifetime.Singleton);
            builder.RegisterEntryPoint<Bootstrapper>();

            MessagePipeOptions messagePipeOptions = builder.RegisterMessagePipe();
            builder.RegisterMessageBroker<AbilityTriggerRequestedEvent>(messagePipeOptions);
            builder.RegisterMessageBroker<AbilityTriggeredEvent>(messagePipeOptions);
            builder.RegisterMessageBroker<AbilityExecutionFailedEvent>(messagePipeOptions);
            builder.RegisterMessageBroker<AbilityCooldownStartedEvent>(messagePipeOptions);
            builder.RegisterMessageBroker<AbilityCooldownUpdatedEvent>(messagePipeOptions);
            builder.RegisterMessageBroker<AbilityCooldownCompletedEvent>(messagePipeOptions);
            builder.RegisterMessageBroker<AbilityEnergyChangedEvent>(messagePipeOptions);

            builder.Register<ICooldownService, CooldownService>(Lifetime.Singleton);
            builder.Register<IEnergyService, EnergyService>(Lifetime.Singleton);
            builder.Register<IAbilityFactory, AbilityFactory>(Lifetime.Singleton);
            builder.Register<AbilityController>(Lifetime.Singleton);
            builder.Register<IAbilityController>(resolver => resolver.Resolve<AbilityController>(), Lifetime.Singleton);
            builder.RegisterEntryPoint<AbilityController>();

            GameObject systems = Instantiate(_defaultSystemsPrefab);
            DontDestroyOnLoad(systems);
        }
    }
}