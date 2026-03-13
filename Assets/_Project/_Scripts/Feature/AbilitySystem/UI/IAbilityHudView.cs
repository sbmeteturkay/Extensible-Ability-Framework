namespace CaseStudy.Feature.AbilitySystem.UI
{
    public interface IAbilityHudView
    {
        void SetEnergy(float currentEnergy, float maxEnergy);

        void SetCooldown(string abilityKey, float remainingSeconds, float normalizedRemaining);

        void ClearCooldown(string abilityKey);
    }
}
