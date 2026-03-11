using System;
using CaseStudy.Feature.AbilitySystem.Contracts;
using UnityEngine;

namespace CaseStudy.Feature.AbilitySystem.Domain
{
    public sealed class AbilityContext
    {
        public AbilityContext(
            Transform ownerTransform,
            Rigidbody ownerRigidbody,
            ICooldownService cooldownService,
            IEnergyService energyService)
        {
            OwnerTransform = ownerTransform ?? throw new ArgumentNullException(nameof(ownerTransform));
            OwnerRigidbody = ownerRigidbody ?? throw new ArgumentNullException(nameof(ownerRigidbody));
            CooldownService = cooldownService ?? throw new ArgumentNullException(nameof(cooldownService));
            EnergyService = energyService ?? throw new ArgumentNullException(nameof(energyService));
        }

        public Transform OwnerTransform { get; }

        public Rigidbody OwnerRigidbody { get; }

        public ICooldownService CooldownService { get; }

        public IEnergyService EnergyService { get; }
    }
}