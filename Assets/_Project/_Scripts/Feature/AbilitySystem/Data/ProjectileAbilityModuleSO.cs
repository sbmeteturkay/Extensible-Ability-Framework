using UnityEngine;

namespace CaseStudy.Feature.AbilitySystem.Data
{
    [CreateAssetMenu(fileName = "SO_Module_Projectile", menuName = "Ability/Modules/Projectile Module")]
    public sealed class ProjectileAbilityModuleSO : AbilityModuleSO
    {
        [SerializeField] private GameObject _projectilePrefab;
        [SerializeField] private GameObject _impactVfxPrefab;
        [SerializeField, Min(0f)] private float _impactVfxDelaySeconds;
        [SerializeField, Min(0f)] private float _impactVfxAutoReturnSeconds = 2f;
        [SerializeField, Min(0f)] private float _projectileSpeed = 16f;
        [SerializeField, Min(0.1f)] private float _maxLifeTimeSeconds = 3f;
        [SerializeField, Min(0f)] private float _hitRadius = 0.1f;
        [SerializeField] private HitVisualProfileSO _targetHitVisualProfile;

        [Header("Spawn")]
        [SerializeField] private float _spawnForwardOffset = 1f;
        [SerializeField] private float _spawnHorizontalOffset;
        [SerializeField] private float _spawnVerticalOffset;

        [Header("Aim")]
        [SerializeField, Min(0.1f)] private float _aimMaxDistance = 60f;
        [SerializeField] private float _aimRayVerticalOffset = 1f;

        public GameObject ProjectilePrefab => _projectilePrefab;

        public GameObject ImpactVfxPrefab => _impactVfxPrefab;

        public float ImpactVfxDelaySeconds => _impactVfxDelaySeconds;

        public float ImpactVfxAutoReturnSeconds => _impactVfxAutoReturnSeconds;

        public float ProjectileSpeed => _projectileSpeed;

        public float MaxLifeTimeSeconds => _maxLifeTimeSeconds;

        public float HitRadius => _hitRadius;

        public HitVisualProfileSO TargetHitVisualProfile => _targetHitVisualProfile;

        public float SpawnForwardOffset => _spawnForwardOffset;

        public float SpawnHorizontalOffset => _spawnHorizontalOffset;

        public float SpawnVerticalOffset => _spawnVerticalOffset;

        public float AimMaxDistance => _aimMaxDistance;

        public float AimRayVerticalOffset => _aimRayVerticalOffset;
    }
}
