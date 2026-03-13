using CaseStudy.Feature.Animation.Runtime;
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
            builder.RegisterComponentInHierarchy<PlayerAnimationDriver>();
        }
    }
}
