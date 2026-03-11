using System.Collections.Generic;
using UnityEngine;

namespace CaseStudy.Feature.AbilitySystem.Abilities
{
    public sealed class ProjectilePool
    {
        private readonly ProjectileRuntime _projectilePrefab;
        private readonly Stack<ProjectileRuntime> _inactiveProjectiles;
        private readonly Transform _poolRoot;
        private readonly int _maxPoolSize;

        private int _liveCount;

        public ProjectilePool(ProjectileRuntime projectilePrefab, int maxPoolSize)
        {
            _projectilePrefab = projectilePrefab;
            _maxPoolSize = Mathf.Max(1, maxPoolSize);
            _inactiveProjectiles = new Stack<ProjectileRuntime>(_maxPoolSize);

            GameObject rootObject = new GameObject($"ProjectilePool_{projectilePrefab.name}");
            _poolRoot = rootObject.transform;
        }

        public ProjectileRuntime Get()
        {
            if (_inactiveProjectiles.Count > 0)
            {
                ProjectileRuntime pooledProjectile = _inactiveProjectiles.Pop();
                if (pooledProjectile != null)
                {
                    pooledProjectile.gameObject.SetActive(true);
                    return pooledProjectile;
                }
            }

            if (_liveCount >= _maxPoolSize)
            {
                return null;
            }

            ProjectileRuntime createdProjectile = Object.Instantiate(_projectilePrefab, _poolRoot);
            _liveCount++;
            createdProjectile.gameObject.SetActive(true);
            return createdProjectile;
        }

        public void Release(ProjectileRuntime projectile)
        {
            if (projectile == null)
            {
                return;
            }

            if (_inactiveProjectiles.Count >= _maxPoolSize)
            {
                Object.Destroy(projectile.gameObject);
                _liveCount = Mathf.Max(0, _liveCount - 1);
                return;
            }

            projectile.transform.SetParent(_poolRoot, false);
            projectile.gameObject.SetActive(false);
            _inactiveProjectiles.Push(projectile);
        }
    }
}