using UnityEngine;

namespace CaseStudy.Feature.AbilitySystem.Data
{
    [CreateAssetMenu(fileName = "SO_AoeAbility", menuName = "Ability/AOE Data")]
    public sealed class AoeAbilityDataSO : AbilityDataSO
    {
        [Header("AOE")]
        [SerializeField, Min(0.1f)] private float _radius = 3f;
        [SerializeField, Min(0f)] private float _effectDurationSeconds = 1f;
        [SerializeField] private GameObject _aoeVfxPrefab;
        [SerializeField] private Color _debugColor = Color.cyan;

        public float Radius => _radius;

        public float EffectDurationSeconds => _effectDurationSeconds;

        public GameObject AoeVfxPrefab => _aoeVfxPrefab;

        public Color DebugColor => _debugColor;
    }
}