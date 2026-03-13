using UnityEngine;

namespace CaseStudy.Feature.AbilitySystem.Data
{
    [CreateAssetMenu(fileName = "SO_AbilitySlot", menuName = "Ability/Slot Definition")]
    public sealed class SlotDefinitionSO : ScriptableObject
    {
        [SerializeField, HideInInspector] private string _slotKey = string.Empty;
        [SerializeField] private string _displayName = "New Slot";

        public string SlotKey => _slotKey;

        public string DisplayName => _displayName;

#if UNITY_EDITOR
        private void OnEnable()
        {
            AssignSlotKeyFromGuid();
        }

        private void OnValidate()
        {
            AssignSlotKeyFromGuid();
        }

        private void AssignSlotKeyFromGuid()
        {
            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return;
            }

            string guid = UnityEditor.AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrWhiteSpace(guid) || guid == _slotKey)
            {
                return;
            }

            _slotKey = guid;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
