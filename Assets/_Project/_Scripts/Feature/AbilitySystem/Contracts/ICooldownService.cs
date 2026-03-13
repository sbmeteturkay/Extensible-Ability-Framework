namespace CaseStudy.Feature.AbilitySystem.Contracts
{
    public interface ICooldownService
    {
        bool IsReady(string abilityKey);

        void StartCooldown(string abilityKey, float durationSeconds);

        float GetRemaining(string abilityKey);
    }
}
