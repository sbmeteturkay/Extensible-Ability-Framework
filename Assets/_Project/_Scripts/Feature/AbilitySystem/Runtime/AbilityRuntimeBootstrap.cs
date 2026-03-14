using System;
using System.Collections.Generic;
using CaseStudy.Feature.AbilitySystem.Contracts;
using CaseStudy.Feature.AbilitySystem.Data;
using CaseStudy.Shared.AbilitySystem.Events;
using CaseStudy.Shared.Locomotion.Interfaces;
using CaseStudy.Shared.Vfx.Interfaces;
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
        private IPooledVfxService _pooledVfxService;

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
                resolver.TryResolve<IPooledVfxService>(out _pooledVfxService);
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

                string slotKey = AbilitySlotKeyUtility.Normalize(entry.SlotKey);
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
                _pooledVfxService,
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

            if (!data.TryValidateConfiguration(out string validationError))
            {
                Debug.LogWarning($"AbilityRuntimeBootstrap: {slotKey} -> {data.name} is invalid. {validationError}");
                return null;
            }

            return data;
        }
    }
}

