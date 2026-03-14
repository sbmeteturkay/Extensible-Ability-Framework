using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace CaseStudy.Feature.AbilitySystem.Data
{
    [CreateAssetMenu(fileName = "SO_Ability", menuName = "Ability/Ability Data")]
    public class AbilityDataSO : ScriptableObject
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
        [SerializeField] private AbilityExecutorSO _executor;
        [SerializeField] private List<AbilityModuleSO> _modules = new();
        [SerializeField] private List<AbilityOverrideSO> _overrides = new();

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

        public AbilityExecutorSO Executor => _executor;

        public IReadOnlyList<AbilityModuleSO> Modules => _modules;

        public IReadOnlyList<AbilityOverrideSO> Overrides => _overrides;

        public GameObject CastVfxPrefab => _castVfxPrefab;

        public AudioClip CastSfx => _castSfx;

        public bool TryGetModule<TModule>(out TModule module) where TModule : AbilityModuleSO
        {
            module = null;
            int count = _modules != null ? _modules.Count : 0;

            for (int i = 0; i < count; i++)
            {
                AbilityModuleSO candidate = _modules[i];
                if (candidate is TModule typedModule)
                {
                    module = typedModule;
                    return true;
                }
            }

            return false;
        }

        public bool TryValidateConfiguration(out string validationError)
        {
            EnsureListsInitialized();

            if (string.IsNullOrWhiteSpace(_abilityKey))
            {
                validationError = "Ability key is empty.";
                return false;
            }

            if (_cooldownSeconds < 0f || _energyCost < 0f || _minimumMovementLockDurationSeconds < 0f)
            {
                validationError = "Cooldown, energy and minimum movement lock duration must be >= 0.";
                return false;
            }

            if (_executor == null)
            {
                validationError = "Executor is missing.";
                return false;
            }

            if (!TryValidateModules(out validationError))
            {
                return false;
            }

            if (!TryValidateOverrides(out validationError))
            {
                return false;
            }

            if (!_executor.TryValidate(this, out validationError))
            {
                return false;
            }

            validationError = string.Empty;
            return true;
        }

#if UNITY_EDITOR
        [ContextMenu("Validate Ability Configuration")]
        private void ValidateAbilityConfiguration()
        {
            if (TryValidateConfiguration(out string validationError))
            {
                Debug.Log($"AbilityDataSO '{name}' validation passed.", this);
            }
            else
            {
                Debug.LogWarning($"AbilityDataSO '{name}' validation failed: {validationError}", this);
            }
        }

        private void OnValidate()
        {
            EnsureListsInitialized();
            AssignAbilityKeyFromGuid();

            if (!TryValidateConfiguration(out string validationError))
            {
                Debug.LogWarning($"AbilityDataSO '{name}' validation failed: {validationError}", this);
            }
        }

        private void OnEnable()
        {
            EnsureListsInitialized();
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

        private void EnsureListsInitialized()
        {
            _modules ??= new List<AbilityModuleSO>();
            _overrides ??= new List<AbilityOverrideSO>();
        }

        private bool TryValidateModules(out string validationError)
        {
            var seenModuleTypes = new HashSet<Type>();
            int count = _modules.Count;

            for (int i = 0; i < count; i++)
            {
                AbilityModuleSO module = _modules[i];
                if (module == null)
                {
                    validationError = $"Modules list contains an empty entry at index {i}.";
                    return false;
                }

                Type moduleType = module.GetType();
                if (!seenModuleTypes.Add(moduleType))
                {
                    validationError = $"Duplicate module type detected: {moduleType.Name}. Keep one module per type.";
                    return false;
                }
            }

            validationError = string.Empty;
            return true;
        }

        private bool TryValidateOverrides(out string validationError)
        {
            int count = _overrides.Count;

            for (int i = 0; i < count; i++)
            {
                AbilityOverrideSO abilityOverride = _overrides[i];
                if (abilityOverride == null)
                {
                    validationError = $"Overrides list contains an empty entry at index {i}.";
                    return false;
                }
            }

            validationError = string.Empty;
            return true;
        }
    }
}
