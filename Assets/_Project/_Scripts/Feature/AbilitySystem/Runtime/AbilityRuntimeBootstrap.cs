using System.Collections.Generic;
using CaseStudy.Feature.AbilitySystem.Contracts;
using CaseStudy.Feature.AbilitySystem.Data;
using CaseStudy.Feature.AbilitySystem.Domain;
using UnityEngine;
using VContainer;

namespace CaseStudy.Feature.AbilitySystem.Runtime
{
    /// <summary>
    ///     Builds ability context and configures the controller from scene references and loadout data.
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
            if (_abilityController == null || _cooldownService == null || _energyService == null)
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

            Dictionary<AbilitySlot, AbilityDataSO> mapping = new(3)
            {
                { AbilitySlot.Primary, _loadout.Get(AbilitySlot.Primary) },
                { AbilitySlot.Secondary, _loadout.Get(AbilitySlot.Secondary) },
                { AbilitySlot.Utility, _loadout.Get(AbilitySlot.Utility) }
            };

            AbilityContext context = new(_ownerTransform, _ownerRigidbody, _cooldownService, _energyService);
            _abilityController.Configure(mapping, context);
        }

        [Inject]
        public void Construct(
            IAbilityController abilityController,
            ICooldownService cooldownService,
            IEnergyService energyService)
        {
            _abilityController = abilityController;
            _cooldownService = cooldownService;
            _energyService = energyService;
        }
    }
}