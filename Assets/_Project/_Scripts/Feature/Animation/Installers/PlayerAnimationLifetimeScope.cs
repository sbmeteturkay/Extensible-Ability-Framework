using CaseStudy.Feature.Animation.Runtime;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace CaseStudy.Feature.Animation.Installers
{
    /// <summary>
    /// Feature-local DI scope for player animation.
    /// </summary>
    public sealed class PlayerAnimationLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            if (!TryGetComponent(out PlayerAnimationDriver playerAnimationDriver))
            {
                Debug.LogWarning("PlayerAnimationLifetimeScope: PlayerAnimationDriver is missing on scope object.", this);
                return;
            }
            builder.RegisterComponent(playerAnimationDriver);
        }
    }
}

