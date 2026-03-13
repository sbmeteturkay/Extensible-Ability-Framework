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
        }
    }
}
