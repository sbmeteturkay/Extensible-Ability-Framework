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

        [Header("Execution")]
        [SerializeField] private AbilityExecutorSO _executor;
        [SerializeField] private AbilityMechanicConfigSO _mechanicConfig;

        [Header("Optional Modules")]
        [SerializeField] private List<AbilityOptionalModuleSO> _optionalModules = new();

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
        public AbilityMechanicConfigSO MechanicConfig => _mechanicConfig;
        public IReadOnlyList<AbilityOptionalModuleSO> OptionalModules => _optionalModules;

        public bool TryGetMechanicConfig<TConfig>(out TConfig mechanicConfig) where TConfig : AbilityMechanicConfigSO
        {
            if (_mechanicConfig is TConfig typedPrimary)
            {
                mechanicConfig = typedPrimary;
                return true;
            }

            mechanicConfig = null;
            return false;
        }

        public bool TryGetModule<TModule>(out TModule module) where TModule : AbilityOptionalModuleSO
        {
            module = null;
            int count = _optionalModules != null ? _optionalModules.Count : 0;

            for (int i = 0; i < count; i++)
            {
                AbilityOptionalModuleSO candidate = _optionalModules[i];
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

            if (_mechanicConfig == null)
            {
                validationError = "Mechanic config is missing. Assign MechanicConfig.";
                return false;
            }

            if (!TryValidateOptionalModules(out validationError))
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
            _optionalModules ??= new List<AbilityOptionalModuleSO>();
        }

        private bool TryValidateOptionalModules(out string validationError)
        {
            var seenModuleTypes = new HashSet<Type>();
            int count = _optionalModules.Count;

            for (int i = 0; i < count; i++)
            {
                AbilityOptionalModuleSO module = _optionalModules[i];
                if (module == null)
                {
                    validationError = $"Optional modules list contains an empty entry at index {i}.";
                    return false;
                }

                Type moduleType = module.GetType();
                if (!seenModuleTypes.Add(moduleType))
                {
                    validationError = $"Duplicate optional module type detected: {moduleType.Name}. Keep one module per type.";
                    return false;
                }
            }

            validationError = string.Empty;
            return true;
        }
    }
}
