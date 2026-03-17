using CaseStudy.Feature.Locomotion.Contracts;
using CaseStudy.Feature.Locomotion.Input;
using CaseStudy.Feature.Locomotion.Runtime;
using CaseStudy.Feature.Locomotion.Services;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace CaseStudy.Feature.Locomotion.Installers
{
    /// <summary>
    /// Feature-local DI scope for locomotion.
    /// Keeps movement logic decoupled from other gameplay systems.
    /// </summary>
    public sealed class LocomotionLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            if (!TryGetComponent(out LocomotionInputGateway locomotionInputGateway) || !TryGetComponent(out LocomotionRuntimeBootstrap locomotionRuntimeBootstrap))
            {
                Debug.LogWarning("LocomotionLifetimeScope: LocomotionInputGateway or LocomotionRuntimeBootstrap is missing on scope object.", this);
                return;
            }

            builder.RegisterComponent(locomotionInputGateway);
            builder.RegisterComponent(locomotionRuntimeBootstrap);

            builder.Register<LocomotionController>(Lifetime.Singleton);
            builder.Register<ILocomotionController>(resolver => resolver.Resolve<LocomotionController>(), Lifetime.Singleton);
            builder.Register<ILocomotionInputReader>(resolver => resolver.Resolve<LocomotionInputGateway>(), Lifetime.Singleton);

            builder.RegisterEntryPoint<LocomotionController>();
        }
    }
}

