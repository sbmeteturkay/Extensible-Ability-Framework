using UnityEngine;

namespace CaseStudy.Feature.AbilitySystem.Data
{
    [CreateAssetMenu(fileName = "SO_Mechanic_Aoe", menuName = "Ability/Mechanics/AOE")]
    public sealed class AoeMechanicConfigSO : AbilityMechanicConfigSO
    {
        [SerializeField, Min(0.1f)] private float _radius = 3f;
        [SerializeField, Min(0f)] private float _hitDelaySeconds;
        [SerializeField, Min(0f)] private float _effectDurationSeconds = 1f;
        [SerializeField, Min(1)] private int _maxTargets = 32;
        [SerializeField] private GameObject _aoeVfxPrefab;
        [SerializeField, Min(0f)] private float _aoeVfxDelaySeconds;
        [SerializeField, Min(0f)] private float _aoeVfxAutoReturnSeconds = 2f;

        public float Radius => _radius;
        public float HitDelaySeconds => _hitDelaySeconds;
        public float EffectDurationSeconds => _effectDurationSeconds;
        public int MaxTargets => _maxTargets;
        public GameObject AoeVfxPrefab => _aoeVfxPrefab;
        public float AoeVfxDelaySeconds => _aoeVfxDelaySeconds;
        public float AoeVfxAutoReturnSeconds => _aoeVfxAutoReturnSeconds;
    }
}
