using System;
using CaseStudy.Feature.AbilitySystem.Contracts;
using CaseStudy.Feature.AbilitySystem.Data;
using CaseStudy.Shared.Locomotion.Interfaces;
using CaseStudy.Shared.Vfx.Interfaces;
using UnityEngine;

namespace CaseStudy.Feature.AbilitySystem.Domain
{
    public sealed class AbilityContext
    {
        public AbilityContext(
            Transform ownerTransform,
            Rigidbody ownerRigidbody,
            ICooldownService cooldownService,
            IEnergyService energyService,
            ILocomotionLockService locomotionLockService,
            IPooledVfxService pooledVfxService,
            AbilityTargetingProfileSO targetingProfile)
        {
            OwnerTransform = ownerTransform ?? throw new ArgumentNullException(nameof(ownerTransform));
            OwnerRigidbody = ownerRigidbody ?? throw new ArgumentNullException(nameof(ownerRigidbody));
            CooldownService = cooldownService ?? throw new ArgumentNullException(nameof(cooldownService));
            EnergyService = energyService ?? throw new ArgumentNullException(nameof(energyService));
            LocomotionLockService = locomotionLockService;
            PooledVfxService = pooledVfxService;
            TargetingProfile = targetingProfile;
        }

        public Transform OwnerTransform { get; }

        public Rigidbody OwnerRigidbody { get; }

        public ICooldownService CooldownService { get; }

        public IEnergyService EnergyService { get; }

        public ILocomotionLockService LocomotionLockService { get; }

        public IPooledVfxService PooledVfxService { get; }

        public AbilityTargetingProfileSO TargetingProfile { get; }

        public LayerMask ResolveTargetLayers(AbilityDataSO abilityData)
        {
            if (abilityData == null)
            {
                return default;
            }

            if (TargetingProfile == null)
            {
                LayerMask allLayers = default;
                allLayers.value = ~0;
                return allLayers;
            }

            return TargetingProfile.Resolve(abilityData.TargetGroups);
        }
    }
}
