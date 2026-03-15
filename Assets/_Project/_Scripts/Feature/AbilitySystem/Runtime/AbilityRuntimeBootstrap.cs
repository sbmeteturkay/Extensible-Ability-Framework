using System;
using System.Collections.Generic;
using CaseStudy.Feature.AbilitySystem.Contracts;
using CaseStudy.Feature.AbilitySystem.Data;
using CaseStudy.Shared.AbilitySystem.Events.Presentation;
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
        [SerializeField] private AudioSource _ownerAudioSource;
        [SerializeField] private AbilityLoadoutSO _loadout;
        [SerializeField] private AbilityTargetingProfileSO _targetingProfile;

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

            if (_ownerAudioSource == null)
            {
                _ownerAudioSource = GetComponent<AudioSource>();
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

            IReadOnlyList<AbilityDataSO> configuredAbilities = _loadout.Abilities;
            if (configuredAbilities == null || configuredAbilities.Count <= 0)
            {
                Debug.LogWarning("AbilityRuntimeBootstrap: no configured abilities in loadout.");
                enabled = false;
                return;
            }

            var mapping = new Dictionary<int, AbilityDataSO>(configuredAbilities.Count);

            for (int slotIndex = 0; slotIndex < configuredAbilities.Count; slotIndex++)
            {
                AbilityDataSO abilityData = configuredAbilities[slotIndex];
                AbilityDataSO validatedData = GetValidatedData(slotIndex, abilityData);
                if (validatedData == null)
                {
                    continue;
                }

                mapping[slotIndex] = validatedData;
            }

            if (mapping.Count <= 0)
            {
                Debug.LogWarning("AbilityRuntimeBootstrap: no valid abilities found in loadout.");
                enabled = false;
                return;
            }

            var context = new Domain.AbilityContext(
                _ownerTransform,
                _ownerRigidbody,
                _ownerAudioSource,
                _cooldownService,
                _energyService,
                _locomotionLockService,
                _pooledVfxService,
                _targetingProfile);

            _abilityController.Configure(mapping, context);

            foreach (KeyValuePair<int, AbilityDataSO> pair in mapping)
            {
                ResolveAbilityAnimation(pair.Value, out AnimationClip abilityAnimationClip, out float abilityAnimationSpeed);
                _slotAssignedPublisher.Publish(new AbilityLoadoutSlotAssignedEvent(
                    pair.Key,
                    pair.Value.AbilityKey,
                    pair.Value.Icon,
                    abilityAnimationClip,
                    abilityAnimationSpeed));
            }
        }

        private static void ResolveAbilityAnimation(AbilityDataSO abilityData, out AnimationClip abilityAnimationClip, out float abilityAnimationSpeed)
        {
            abilityAnimationClip = null;
            abilityAnimationSpeed = 1f;

            if (abilityData == null || !abilityData.TryGetModule(out AbilityAnimationModuleSO animationModule))
            {
                return;
            }

            abilityAnimationClip = animationModule.AnimationClip;
            if (animationModule.UseCustomPlaybackSpeed)
            {
                abilityAnimationSpeed = Mathf.Max(0.01f, animationModule.PlaybackSpeed);
            }
        }

        private AbilityDataSO GetValidatedData(int slotIndex, AbilityDataSO data)
        {
            if (data == null)
            {
                Debug.LogWarning($"AbilityRuntimeBootstrap: slot {slotIndex} has no ability data.");
                return null;
            }

            if (!data.TryValidateConfiguration(out string validationError))
            {
                Debug.LogWarning($"AbilityRuntimeBootstrap: slot {slotIndex} -> {data.name} is invalid. {validationError}");
                return null;
            }

            return data;
        }
    }
}


