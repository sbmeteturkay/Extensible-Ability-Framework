using CaseStudy.Feature.AbilitySystem.Domain;

namespace CaseStudy.Feature.AbilitySystem.Contracts
{
    public interface ICooldownService
    {
        bool IsReady(AbilityId abilityId);

        void StartCooldown(AbilityId abilityId, float durationSeconds);

        float GetRemaining(AbilityId abilityId);
    }
}