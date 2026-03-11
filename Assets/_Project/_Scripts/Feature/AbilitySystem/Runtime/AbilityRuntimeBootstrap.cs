using System.Collections.Generic;
using CaseStudy.Feature.AbilitySystem.Abilities;
using CaseStudy.Feature.AbilitySystem.Contracts;
using CaseStudy.Feature.AbilitySystem.Data;
using CaseStudy.Feature.AbilitySystem.Domain;
using CaseStudy.Shared.AbilitySystem.Events;
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

        private IAbilityController _abilityController;
        private ICooldownService _cooldownService;
        private IEnergyService _energyService;
        private IPublisher<AbilityLoadoutSlotAssignedEvent> _slotAssignedPublisher;

        [Inject]
        public void Construct(
            IAbilityController abilityController,
            ICooldownService cooldownService,
            IEnergyService energyService,
            IPublisher<AbilityLoadoutSlotAssignedEvent> slotAssignedPublisher)
        {
            _abilityController = abilityController;
            _cooldownService = cooldownService;
            _energyService = energyService;
            _slotAssignedPublisher = slotAssignedPublisher;
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

            var mapping = new Dictionary<AbilitySlot, AbilityDataSO>(3)
            {
                { AbilitySlot.Primary, GetValidatedData(AbilitySlot.Primary) },
                { AbilitySlot.Secondary, GetValidatedData(AbilitySlot.Secondary) },
                { AbilitySlot.Utility, GetValidatedData(AbilitySlot.Utility) }
            };

            var context = new AbilityContext(_ownerTransform, _ownerRigidbody, _cooldownService, _energyService);
            _abilityController.Configure(mapping, context);

            PublishSlotIfAvailable(AbilitySlot.Primary, mapping[AbilitySlot.Primary]);
            PublishSlotIfAvailable(AbilitySlot.Secondary, mapping[AbilitySlot.Secondary]);
            PublishSlotIfAvailable(AbilitySlot.Utility, mapping[AbilitySlot.Utility]);
        }

        private AbilityDataSO GetValidatedData(AbilitySlot slot)
        {
            AbilityDataSO data = _loadout.Get(slot);
            if (data == null)
            {
                Debug.LogWarning($"AbilityRuntimeBootstrap: {slot} slot has no ability data.");
                return null;
            }

            if (data.AbilityId == AbilityId.None)
            {
                Debug.LogWarning($"AbilityRuntimeBootstrap: {slot} uses {data.name} with AbilityId.None.");
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

                if (projectileData.MaxPoolSize <= 0)
                {
                    Debug.LogWarning($"AbilityRuntimeBootstrap: {data.name} has invalid pool size.");
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

        private void PublishSlotIfAvailable(AbilitySlot slot, AbilityDataSO data)
        {
            if (data == null)
            {
                return;
            }

            _slotAssignedPublisher.Publish(new AbilityLoadoutSlotAssignedEvent(slot, data.AbilityId, data.Icon));
        }
    }
}