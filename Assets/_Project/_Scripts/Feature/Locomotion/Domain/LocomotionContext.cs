using System;
using CaseStudy.Feature.Locomotion.Contracts;
using CaseStudy.Feature.Locomotion.Data;
using UnityEngine;

namespace CaseStudy.Feature.Locomotion.Domain
{
    public sealed class LocomotionContext
    {
        public LocomotionContext(
            Transform ownerTransform,
            Rigidbody ownerRigidbody,
            ILocomotionInputReader inputReader,
            LocomotionDataSO locomotionData)
        {
            OwnerTransform = ownerTransform ?? throw new ArgumentNullException(nameof(ownerTransform));
            OwnerRigidbody = ownerRigidbody ?? throw new ArgumentNullException(nameof(ownerRigidbody));
            InputReader = inputReader ?? throw new ArgumentNullException(nameof(inputReader));
            LocomotionData = locomotionData ?? throw new ArgumentNullException(nameof(locomotionData));
        }

        public Transform OwnerTransform { get; }

        public Rigidbody OwnerRigidbody { get; }

        public ILocomotionInputReader InputReader { get; }

        public LocomotionDataSO LocomotionData { get; }
    }
}
