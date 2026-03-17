using CaseStudy.Feature.AbilitySystem.UI;
using VContainer;
using VContainer.Unity;

namespace CaseStudy.Feature.AbilitySystem.Installers
{
    /// <summary>
    /// Scene-side scope for ability HUD presentation.
    /// Place this under the scene UI hierarchy.
    /// </summary>
    public sealed class AbilitySceneLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<AbilityHudPresenter>();
        }
    }
}

