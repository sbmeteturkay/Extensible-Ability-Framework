using System;
using System.Collections.Generic;
using UnityEngine;

namespace CaseStudy.Feature.AbilitySystem.Data
{
    /// <summary>
    /// Defines which ability data is assigned to each runtime slot.
    /// Uses slot definition assets for typo-safe binding.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_AbilityLoadout", menuName = "Ability/Loadout")]
    public sealed class AbilityLoadoutSO : ScriptableObject
    {
        [Serializable]
        public sealed class AbilityLoadoutEntry
        {
            [SerializeField] private SlotDefinitionSO _slot;
            [SerializeField] private AbilityDataSO _abilityData;

            public SlotDefinitionSO Slot => _slot;

            public string SlotKey => _slot != null ? NormalizeKey(_slot.SlotKey) : string.Empty;

            public AbilityDataSO AbilityData => _abilityData;

            private static string NormalizeKey(string key)
            {
                return string.IsNullOrWhiteSpace(key) ? string.Empty : key.Trim();
            }
        }

        [SerializeField] private List<AbilityLoadoutEntry> _slots = new(3);

        public IReadOnlyList<AbilityLoadoutEntry> Slots => _slots;

        public int GetConfiguredSlots(List<AbilityLoadoutEntry> output)
        {
            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            output.Clear();
            if (_slots == null)
            {
                return 0;
            }

            int count = _slots.Count;
            for (int i = 0; i < count; i++)
            {
                AbilityLoadoutEntry entry = _slots[i];
                if (entry == null || entry.Slot == null || entry.AbilityData == null || string.IsNullOrWhiteSpace(entry.SlotKey))
                {
                    continue;
                }

                output.Add(entry);
            }

            return output.Count;
        }
    }
}
