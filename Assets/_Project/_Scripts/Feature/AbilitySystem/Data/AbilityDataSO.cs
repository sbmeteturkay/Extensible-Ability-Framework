using UnityEngine;
using UnityEngine.Serialization;

namespace CaseStudy.Feature.AbilitySystem.Data
{
    public abstract class AbilityDataSO : ScriptableObject
    {
        [Header("Common")]
        [SerializeField, HideInInspector, FormerlySerializedAs("_abilityId")] private string _abilityKey = string.Empty;
        [SerializeField] private string _displayName = "New Ability";
        [SerializeField] private Sprite _icon;
        [SerializeField, Min(0f)] private float _cooldownSeconds = 1f;
        [SerializeField, Min(0f)] private float _energyCost;
        [SerializeField] private AbilityTargetGroups _targetGroups = AbilityTargetGroups.HitEnemies;
        [SerializeField] private AbilityMovementPolicy _movementPolicy = AbilityMovementPolicy.None;
        [SerializeField, Min(0f)] private float _minimumMovementLockDurationSeconds;

        [Header("Feedback")]
        [SerializeField] private GameObject _castVfxPrefab;
        [SerializeField] private AudioClip _castSfx;

        public string AbilityKey => _abilityKey;

        public string DisplayName => _displayName;

        public Sprite Icon => _icon;

        public float CooldownSeconds => _cooldownSeconds;

        public float EnergyCost => _energyCost;

        public AbilityTargetGroups TargetGroups => _targetGroups;

        public virtual AbilityMovementPolicy MovementPolicy => _movementPolicy;

        public bool ShouldLockLocomotion => MovementPolicy == AbilityMovementPolicy.LockLocomotionDuringExecution;

        public float MinimumMovementLockDurationSeconds => _minimumMovementLockDurationSeconds;

        public GameObject CastVfxPrefab => _castVfxPrefab;

        public AudioClip CastSfx => _castSfx;

#if UNITY_EDITOR
        private void OnValidate()
        {
            AssignAbilityKeyFromGuid();
        }

        private void OnEnable()
        {
            AssignAbilityKeyFromGuid();
        }

        private void AssignAbilityKeyFromGuid()
        {
            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return;
            }

            string guid = UnityEditor.AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrWhiteSpace(guid) || guid == _abilityKey)
            {
                return;
            }

            _abilityKey = guid;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
