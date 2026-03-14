namespace CaseStudy.Feature.AbilitySystem.UI
{
    public interface IAbilityHudView
    {
        void SetEnergy(float currentEnergy, float maxEnergy);

        void SetCooldown(int slotIndex, float remainingSeconds, float normalizedRemaining);

        void ClearCooldown(int slotIndex);
    }
}
