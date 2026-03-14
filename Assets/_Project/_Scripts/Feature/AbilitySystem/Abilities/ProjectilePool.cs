using System.Collections.Generic;
using CaseStudy.Shared.Pooling;
using UnityEngine;

namespace CaseStudy.Feature.AbilitySystem.Abilities
{
    public sealed class ProjectilePool
    {
        private readonly ProjectileRuntime _projectilePrefab;
        private readonly Stack<ProjectileRuntime> _inactiveProjectiles = new();
        private readonly Transform _poolRoot;
        private readonly int _maxPoolSize;

        public ProjectilePool(ProjectileRuntime projectilePrefab, int maxPoolSize)
        {
            _projectilePrefab = projectilePrefab;
            _maxPoolSize = maxPoolSize;

            GameObject rootObject = new GameObject($"ProjectilePool_{projectilePrefab.name}");
            _poolRoot = rootObject.transform;
            _poolRoot.SetParent(PoolRootRegistry.GetAbilityPoolsRoot(), false);
        }

        public ProjectileRuntime Get()
        {
            while (_inactiveProjectiles.Count > 0)
            {
                ProjectileRuntime pooledProjectile = _inactiveProjectiles.Pop();
                if (pooledProjectile != null)
                {
                    pooledProjectile.gameObject.SetActive(true);
                    return pooledProjectile;
                }
            }

            ProjectileRuntime createdProjectile = Object.Instantiate(_projectilePrefab, _poolRoot);
            createdProjectile.gameObject.SetActive(true);
            return createdProjectile;
        }

        public void Release(ProjectileRuntime projectile)
        {
            if (projectile == null)
            {
                return;
            }

            if (_maxPoolSize >= 0 && _inactiveProjectiles.Count >= _maxPoolSize)
            {
                Object.Destroy(projectile.gameObject);
                return;
            }

            projectile.transform.SetParent(_poolRoot, false);
            projectile.gameObject.SetActive(false);
            _inactiveProjectiles.Push(projectile);
        }
    }
}
