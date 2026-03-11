using UnityEngine;

namespace CaseStudy.Feature.AbilitySystem.Data
{
    [CreateAssetMenu(fileName = "SO_ProjectileAbility", menuName = "Ability/Projectile Data")]
    public sealed class ProjectileAbilityDataSO : AbilityDataSO
    {
        [Header("Projectile")]
        [SerializeField] private GameObject _projectilePrefab;
        [SerializeField] private GameObject _impactVfxPrefab;
        [SerializeField, Min(0f)] private float _projectileSpeed = 16f;
        [SerializeField, Min(0.1f)] private float _maxLifeTimeSeconds = 3f;
        [SerializeField, Min(1)] private int _maxPoolSize = 24;
        [SerializeField] private float _spawnForwardOffset = 1f;

        public GameObject ProjectilePrefab => _projectilePrefab;

        public GameObject ImpactVfxPrefab => _impactVfxPrefab;

        public float ProjectileSpeed => _projectileSpeed;

        public float MaxLifeTimeSeconds => _maxLifeTimeSeconds;

        public int MaxPoolSize => _maxPoolSize;

        public float SpawnForwardOffset => _spawnForwardOffset;
    }
}