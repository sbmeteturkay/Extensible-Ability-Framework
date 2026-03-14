namespace CaseStudy.Feature.AbilitySystem
{
    public static class AbilitySlotKeyUtility
    {
        public static string Normalize(string key)
        {
            return string.IsNullOrWhiteSpace(key) ? string.Empty : key.Trim();
        }
    }
}
