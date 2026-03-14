using UnityEngine;

namespace CaseStudy.Feature.AbilitySystem.Data
{
    [CreateAssetMenu(fileName = "SO_Module_Aoe", menuName = "Ability/Modules/AOE Module")]
    public sealed class AoeAbilityModuleSO : AbilityModuleSO
    {
        [SerializeField, Min(0.1f)] private float _radius = 3f;
        [SerializeField, Min(0f)] private float _hitDelaySeconds;
        [SerializeField, Min(0f)] private float _effectDurationSeconds = 1f;
        [SerializeField, Min(1)] private int _maxTargets = 32;
        [SerializeField] private GameObject _aoeVfxPrefab;
        [SerializeField, Min(0f)] private float _aoeVfxDelaySeconds;
        [SerializeField, Min(0f)] private float _aoeVfxAutoReturnSeconds = 2f;
        [SerializeField] private HitVisualProfileSO _targetHitVisualProfile;

        public float Radius => _radius;

        public float HitDelaySeconds => _hitDelaySeconds;

        public float EffectDurationSeconds => _effectDurationSeconds;

        public int MaxTargets => _maxTargets;

        public GameObject AoeVfxPrefab => _aoeVfxPrefab;

        public float AoeVfxDelaySeconds => _aoeVfxDelaySeconds;

        public float AoeVfxAutoReturnSeconds => _aoeVfxAutoReturnSeconds;

        public HitVisualProfileSO TargetHitVisualProfile => _targetHitVisualProfile;
    }
}
