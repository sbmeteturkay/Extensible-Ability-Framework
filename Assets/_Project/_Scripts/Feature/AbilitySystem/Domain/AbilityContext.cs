using System;
using CaseStudy.Feature.AbilitySystem.Contracts;
using CaseStudy.Shared.Locomotion.Interfaces;
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
            ILocomotionLockService locomotionLockService)
        {
            OwnerTransform = ownerTransform ?? throw new ArgumentNullException(nameof(ownerTransform));
            OwnerRigidbody = ownerRigidbody ?? throw new ArgumentNullException(nameof(ownerRigidbody));
            CooldownService = cooldownService ?? throw new ArgumentNullException(nameof(cooldownService));
            EnergyService = energyService ?? throw new ArgumentNullException(nameof(energyService));
            LocomotionLockService = locomotionLockService;
        }

        public Transform OwnerTransform { get; }

        public Rigidbody OwnerRigidbody { get; }

        public ICooldownService CooldownService { get; }

        public IEnergyService EnergyService { get; }

        public ILocomotionLockService LocomotionLockService { get; }
    }
}
