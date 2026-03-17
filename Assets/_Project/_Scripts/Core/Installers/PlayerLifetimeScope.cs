using CaseStudy.Shared.Locomotion.Interfaces;
using CaseStudy.Shared.Locomotion.Services;
using CaseStudy.Shared.PlayerControl.Contracts;
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
            MonoBehaviour[] components = GetComponentsInChildren<MonoBehaviour>(true);
            MonoBehaviour controlNodeComponent = null;
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] is IPlayerControlNode)
                {
                    controlNodeComponent = components[i];
                    break;
                }
            }

            if (controlNodeComponent == null)
            {
                Debug.LogWarning("PlayerLifetimeScope: IPlayerControlNode is missing on player root.", this);
            }
            else
            {
                builder.RegisterComponent(controlNodeComponent);
            }

            builder.Register<LocomotionLockService>(Lifetime.Singleton);
            builder.Register<ILocomotionLockService>(resolver => resolver.Resolve<LocomotionLockService>(), Lifetime.Singleton);
        }
    }
}
