using System;
using System.Collections.Generic;
using CaseStudy.Feature.AbilitySystem.Abilities;
using CaseStudy.Feature.AbilitySystem.Contracts;
using CaseStudy.Feature.AbilitySystem.Data;
using CaseStudy.Shared.AbilitySystem.Events;
using CaseStudy.Shared.Locomotion.Interfaces;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace CaseStudy.Feature.AbilitySystem.Runtime
{
    /// <summary>
    /// Builds ability context and configures the controller from scene references and loadout data.
    /// Invalid or missing ability configs are skipped to keep runtime stable.
    /// </summary>
    public sealed class AbilityRuntimeBootstrap : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _ownerTransform;
        [SerializeField] private Rigidbody _ownerRigidbody;
        [SerializeField] private AbilityLoadoutSO _loadout;
        [SerializeField] private AbilityTargetingProfileSO _targetingProfile;

        private readonly List<AbilityLoadoutSO.AbilityLoadoutEntry> _loadoutBuffer = new(4);

        private IAbilityController _abilityController;
        private ICooldownService _cooldownService;
        private IEnergyService _energyService;
        private IPublisher<AbilityLoadoutSlotAssignedEvent> _slotAssignedPublisher;
        private ILocomotionLockService _locomotionLockService;

        [Inject]
        public void Construct(
            IAbilityController abilityController,
            ICooldownService cooldownService,
            IEnergyService energyService,
            IPublisher<AbilityLoadoutSlotAssignedEvent> slotAssignedPublisher,
            IObjectResolver resolver)
        {
            _abilityController = abilityController;
            _cooldownService = cooldownService;
            _energyService = energyService;
            _slotAssignedPublisher = slotAssignedPublisher;

            if (resolver != null)
            {
                resolver.TryResolve<ILocomotionLockService>(out _locomotionLockService);
            }
        }

        private void Awake()
        {
            if (_ownerTransform == null)
            {
                _ownerTransform = transform;
            }

            if (_ownerRigidbody == null)
            {
                _ownerRigidbody = GetComponent<Rigidbody>();
            }
        }

        private void Start()
        {
            if (_abilityController == null || _cooldownService == null || _energyService == null || _slotAssignedPublisher == null)
            {
                Debug.LogWarning("AbilityRuntimeBootstrap: dependencies were not injected.");
                enabled = false;
                return;
            }

            if (_ownerTransform == null || _ownerRigidbody == null)
            {
                Debug.LogWarning("AbilityRuntimeBootstrap: owner transform/rigidbody is missing.");
                enabled = false;
                return;
            }

            if (_loadout == null)
            {
                Debug.LogWarning("AbilityRuntimeBootstrap: loadout asset is missing.");
                enabled = false;
                return;
            }

            if (_targetingProfile == null)
            {
                Debug.LogWarning("AbilityRuntimeBootstrap: targeting profile is not set. Ability target layers will fallback to all layers.");
            }

            int slotCount = _loadout.GetConfiguredSlots(_loadoutBuffer);
            if (slotCount <= 0)
            {
                Debug.LogWarning("AbilityRuntimeBootstrap: no configured ability slots in loadout.");
                enabled = false;
                return;
            }

            var mapping = new Dictionary<string, AbilityDataSO>(slotCount, StringComparer.Ordinal);

            for (int i = 0; i < slotCount; i++)
            {
                AbilityLoadoutSO.AbilityLoadoutEntry entry = _loadoutBuffer[i];
                if (entry == null)
                {
                    continue;
                }

                string slotKey = NormalizeKey(entry.SlotKey);
                AbilityDataSO validatedData = GetValidatedData(slotKey, entry.AbilityData);
                if (validatedData == null)
                {
                    continue;
                }

                if (mapping.ContainsKey(slotKey))
                {
                    Debug.LogWarning($"AbilityRuntimeBootstrap: duplicate slot key '{slotKey}' in loadout. Last ability wins.");
                }

                mapping[slotKey] = validatedData;
            }

            var context = new Domain.AbilityContext(
                _ownerTransform,
                _ownerRigidbody,
                _cooldownService,
                _energyService,
                _locomotionLockService,
                _targetingProfile);

            _abilityController.Configure(mapping, context);

            foreach (KeyValuePair<string, AbilityDataSO> pair in mapping)
            {
                _slotAssignedPublisher.Publish(new AbilityLoadoutSlotAssignedEvent(pair.Key, pair.Value.AbilityKey, pair.Value.Icon));
            }
        }

        private AbilityDataSO GetValidatedData(string slotKey, AbilityDataSO data)
        {
            if (string.IsNullOrWhiteSpace(slotKey))
            {
                Debug.LogWarning("AbilityRuntimeBootstrap: encountered loadout entry with empty slot key.");
                return null;
            }

            if (data == null)
            {
                Debug.LogWarning($"AbilityRuntimeBootstrap: {slotKey} slot has no ability data.");
                return null;
            }

            if (string.IsNullOrWhiteSpace(data.AbilityKey))
            {
                Debug.LogWarning($"AbilityRuntimeBootstrap: {slotKey} uses {data.name} with empty AbilityKey.");
                return null;
            }

            if (data.CooldownSeconds < 0f || data.EnergyCost < 0f)
            {
                Debug.LogWarning($"AbilityRuntimeBootstrap: {data.name} has negative cooldown/energy values.");
                return null;
            }

            if (data is ProjectileAbilityDataSO projectileData)
            {
                if (projectileData.ProjectilePrefab == null)
                {
                    Debug.LogWarning($"AbilityRuntimeBootstrap: {data.name} projectile prefab is missing.");
                    return null;
                }

                if (projectileData.ProjectilePrefab.GetComponent<ProjectileRuntime>() == null)
                {
                    Debug.LogWarning($"AbilityRuntimeBootstrap: {data.name} projectile prefab must include ProjectileRuntime.");
                    return null;
                }
                if (data.TargetGroups == AbilityTargetGroups.None)
                {
                    Debug.LogWarning($"AbilityRuntimeBootstrap: {data.name} has no target groups selected.");
                    return null;
                }
            }

            if (data is AoeAbilityDataSO aoeData)
            {
                if (aoeData.Radius <= 0f || aoeData.MaxTargets <= 0)
                {
                    Debug.LogWarning($"AbilityRuntimeBootstrap: {data.name} has invalid AOE radius/maxTargets.");
                    return null;
                }

                if (data.TargetGroups == AbilityTargetGroups.None)
                {
                    Debug.LogWarning($"AbilityRuntimeBootstrap: {data.name} has no target groups selected.");
                    return null;
                }
            }

            if (data is DashAbilityDataSO dashData)
            {
                if (dashData.DashDistance <= 0f || dashData.DashDurationSeconds <= 0f)
                {
                    Debug.LogWarning($"AbilityRuntimeBootstrap: {data.name} has invalid dash distance/duration.");
                    return null;
                }
            }

            return data;
        }

        private static string NormalizeKey(string key)
        {
            return string.IsNullOrWhiteSpace(key) ? string.Empty : key.Trim();
        }
    }
}

