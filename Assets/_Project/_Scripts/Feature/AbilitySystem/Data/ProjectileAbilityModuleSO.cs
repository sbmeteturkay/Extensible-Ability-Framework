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
        [SerializeField] private float _spawnForwardOffset = 1f;

        public GameObject ProjectilePrefab => _projectilePrefab;

        public GameObject ImpactVfxPrefab => _impactVfxPrefab;

        public float ImpactVfxDelaySeconds => _impactVfxDelaySeconds;

        public float ImpactVfxAutoReturnSeconds => _impactVfxAutoReturnSeconds;

        public float ProjectileSpeed => _projectileSpeed;

        public float MaxLifeTimeSeconds => _maxLifeTimeSeconds;

        public float SpawnForwardOffset => _spawnForwardOffset;
    }
}
