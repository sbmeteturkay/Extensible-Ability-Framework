using System.Collections.Generic;
using UnityEngine;

namespace CaseStudy.Feature.AbilitySystem.Data
{
    /// <summary>
    /// Defines ordered ability list for runtime slots.
    /// Slot index is the list index.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_AbilityLoadout", menuName = "Ability/Loadout")]
    public sealed class AbilityLoadoutSO : ScriptableObject
    {
        [SerializeField] private List<AbilityDataSO> _abilities = new(3);

        public IReadOnlyList<AbilityDataSO> Abilities => _abilities;

        public int SlotCount => _abilities != null ? _abilities.Count : 0;

        public AbilityDataSO GetAbilityAt(int slotIndex)
        {
            if (_abilities == null || slotIndex < 0 || slotIndex >= _abilities.Count)
            {
                return null;
            }

            return _abilities[slotIndex];
        }
    }
}
