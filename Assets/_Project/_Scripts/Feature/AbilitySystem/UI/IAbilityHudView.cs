using CaseStudy.Feature.AbilitySystem.Domain;

namespace CaseStudy.Feature.AbilitySystem.UI
{
    public interface IAbilityHudView
    {
        void SetEnergy(float currentEnergy, float maxEnergy);

        void SetCooldown(AbilityId abilityId, float remainingSeconds, float normalizedRemaining);

        void ClearCooldown(AbilityId abilityId);
    }
}