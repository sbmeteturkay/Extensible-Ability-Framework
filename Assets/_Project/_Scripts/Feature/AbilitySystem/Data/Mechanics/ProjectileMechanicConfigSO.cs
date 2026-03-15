using UnityEngine;

namespace CaseStudy.Feature.AbilitySystem.Data
{
    [CreateAssetMenu(fileName = "SO_Mechanic_Projectile", menuName = "Ability/Mechanics/Projectile")]
    public sealed class ProjectileMechanicConfigSO : AbilityMechanicConfigSO
    {
        [SerializeField] private GameObject _projectilePrefab;
        [SerializeField] private int _maxPoolSize = -1;
        [SerializeField, Min(0f)] private float _projectileSpeed = 16f;
        [SerializeField, Min(0.1f)] private float _maxLifeTimeSeconds = 3f;
        [SerializeField, Min(0f)] private float _hitRadius = 0.1f;

        [Header("Spawn")]
        [SerializeField] private float _spawnForwardOffset = 1f;
        [SerializeField] private float _spawnHorizontalOffset;
        [SerializeField] private float _spawnVerticalOffset;

        [Header("Aim")]
        [SerializeField, Min(0.1f)] private float _aimMaxDistance = 60f;
        [SerializeField] private float _aimRayVerticalOffset = 1f;

        public GameObject ProjectilePrefab => _projectilePrefab;
        public int MaxPoolSize => _maxPoolSize;
        public float ProjectileSpeed => _projectileSpeed;
        public float MaxLifeTimeSeconds => _maxLifeTimeSeconds;
        public float HitRadius => _hitRadius;
        public float SpawnForwardOffset => _spawnForwardOffset;
        public float SpawnHorizontalOffset => _spawnHorizontalOffset;
        public float SpawnVerticalOffset => _spawnVerticalOffset;
        public float AimMaxDistance => _aimMaxDistance;
        public float AimRayVerticalOffset => _aimRayVerticalOffset;
    }
}
