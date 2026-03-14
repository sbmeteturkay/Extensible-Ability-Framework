namespace CaseStudy.Feature.AbilitySystem.UI
{
    /// <summary>
    /// Fallback HUD view used when no scene HUD is assigned.
    /// </summary>
    public sealed class NullAbilityHudView : IAbilityHudView
    {
        public void SetEnergy(float currentEnergy, float maxEnergy)
        {
        }

        public void SetCooldown(int slotIndex, float remainingSeconds, float normalizedRemaining)
        {
        }

        public void ClearCooldown(int slotIndex)
        {
        }
    }
}
