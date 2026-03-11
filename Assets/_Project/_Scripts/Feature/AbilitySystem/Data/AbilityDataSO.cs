using CaseStudy.Feature.AbilitySystem.Domain;
using UnityEngine;

namespace CaseStudy.Feature.AbilitySystem.Data
{
    public abstract class AbilityDataSO : ScriptableObject
    {
        [Header("Common")]
        //todo: auto ability id set for each inherit
        [SerializeField] private AbilityId _abilityId = AbilityId.None;
        [SerializeField] private string _displayName = "New Ability";
        [SerializeField] private Sprite _icon;
        [SerializeField, Min(0f)] private float _cooldownSeconds = 1f;
        [SerializeField, Min(0f)] private float _energyCost;
        [SerializeField] private LayerMask _affectedLayers = ~0;

        [Header("Feedback")]
        [SerializeField] private GameObject _castVfxPrefab;
        [SerializeField] private AudioClip _castSfx;

        public AbilityId AbilityId => _abilityId;

        public string DisplayName => _displayName;

        public Sprite Icon => _icon;

        public float CooldownSeconds => _cooldownSeconds;

        public float EnergyCost => _energyCost;

        public LayerMask AffectedLayers => _affectedLayers;

        public GameObject CastVfxPrefab => _castVfxPrefab;

        public AudioClip CastSfx => _castSfx;
    }
}