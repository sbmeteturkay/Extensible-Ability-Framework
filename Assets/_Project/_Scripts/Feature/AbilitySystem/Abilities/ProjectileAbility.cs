using System;
using System.Threading;
using CaseStudy.Feature.AbilitySystem.Data;
using CaseStudy.Shared.Vfx.Interfaces;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CaseStudy.Feature.AbilitySystem.Abilities
{
    public sealed class ProjectileAbility : BaseAbility
    {
        private readonly IPooledVfxService _pooledVfxService;

        private ProjectileAbilityDataSO _projectileData;
        private ProjectilePool _projectilePool;

        public ProjectileAbility(IPooledVfxService pooledVfxService)
        {
            _pooledVfxService = pooledVfxService;
        }

        public override void Initialize(Domain.AbilityContext context, AbilityDataSO data)
        {
            base.Initialize(context, data);
            _projectileData = data as ProjectileAbilityDataSO;

            if (_projectileData == null || _projectileData.ProjectilePrefab == null)
            {
                return;
            }

            ProjectileRuntime projectilePrefab = _projectileData.ProjectilePrefab.GetComponent<ProjectileRuntime>();
            if (projectilePrefab == null)
            {
                return;
            }

            _projectilePool = new ProjectilePool(projectilePrefab);
        }

        public override bool CanExecute()
        {
            return base.CanExecute()
                   && _projectileData != null
                   && _projectileData.ProjectilePrefab != null
                   && _projectilePool != null;
        }

        public override UniTask ExecuteAsync(CancellationToken cancellationToken)
        {
            if (!CanExecute())
            {
                throw new InvalidOperationException("Projectile ability is not properly configured.");
            }

            Vector3 direction = Context.OwnerTransform.forward;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = Vector3.forward;
            }

            Vector3 spawnPosition = Context.OwnerTransform.position + direction.normalized * _projectileData.SpawnForwardOffset;
            ProjectileRuntime projectile = _projectilePool.Get();

            LayerMask hitLayers = Context.ResolveTargetLayers(_projectileData);

            projectile.Launch(
                spawnPosition,
                direction,
                _projectileData.ProjectileSpeed,
                _projectileData.MaxLifeTimeSeconds,
                hitLayers,
                _projectileData.ImpactVfxPrefab,
                _projectileData.ImpactVfxDelaySeconds,
                _projectileData.ImpactVfxAutoReturnSeconds,
                _pooledVfxService,
                _projectilePool.Release);

            return UniTask.CompletedTask;
        }
    }
}
