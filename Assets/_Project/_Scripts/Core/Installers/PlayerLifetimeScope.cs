using CaseStudy.Core.PlayerControl.Runtime;
using CaseStudy.Shared.Locomotion.Interfaces;
using CaseStudy.Shared.Locomotion.Services;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace CaseStudy.Core.Installers
{
    /// <summary>
    /// Player-root scope for spawned player prefabs.
    /// Child feature scopes inherit shared registrations from gameplay scope through this node.
    /// </summary>
    public sealed class PlayerLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            PlayerControlState playerControlState = GetComponentInChildren<PlayerControlState>();
            if (playerControlState == null)
            {
                Debug.LogWarning("PlayerLifetimeScope: PlayerControlState is missing on player root.", this);
            }
            else
            {
                builder.RegisterComponent(playerControlState);
            }

            builder.Register<LocomotionLockService>(Lifetime.Singleton);
            builder.Register<ILocomotionLockService>(resolver => resolver.Resolve<LocomotionLockService>(), Lifetime.Singleton);
        }
    }
}
