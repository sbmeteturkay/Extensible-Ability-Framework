using UnityEngine;
using CaseStudy.Feature.AbilitySystem.Domain;

namespace CaseStudy.Feature.AbilitySystem.Data
{
    /// <summary>
    /// Defines which ability data is assigned to each runtime slot.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_AbilityLoadout", menuName = "Ability/Loadout")]
    public sealed class AbilityLoadoutSO : ScriptableObject
    {
        [SerializeField] private AbilityDataSO _primary;
        [SerializeField] private AbilityDataSO _secondary;
        [SerializeField] private AbilityDataSO _utility;

        public AbilityDataSO Get(AbilitySlot slot)
        {
            switch (slot)
            {
                case AbilitySlot.Primary:
                    return _primary;
                case AbilitySlot.Secondary:
                    return _secondary;
                case AbilitySlot.Utility:
                    return _utility;
                default:
                    return null;
            }
        }
    }
}