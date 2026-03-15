using System;
using System.Collections.Generic;
using System.Threading;
using CaseStudy.Feature.AbilitySystem.Abilities;
using CaseStudy.Feature.AbilitySystem.Domain;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CaseStudy.Feature.AbilitySystem.Data
{
    [CreateAssetMenu(fileName = "SO_Executor_Projectile", menuName = "Ability/Executors/Projectile Executor")]
    public sealed class ProjectileExecutorSO : AbilityExecutorSO
    {
        public override Type RequiredMechanicConfigType => typeof(ProjectileMechanicConfigSO);
        private readonly Dictionary<(int PrefabKey, int MaxPoolSize), ProjectilePool> _poolByKey = new();

        public override bool CanExecute(AbilityContext context, AbilityDataSO data)
        {
            return base.CanExecute(context, data)
                   && TryResolveParameters(data, out ProjectileParameters parameters)
                   && IsValid(parameters)
                   && data.TargetGroups != AbilityTargetGroups.None;
        }

        public override UniTask ExecuteAsync(AbilityContext context, AbilityDataSO data, CancellationToken cancellationToken)
        {
            if (!CanExecute(context, data) || !TryResolveParameters(data, out ProjectileParameters parameters))
            {
                throw new InvalidOperationException("ProjectileExecutorSO is not properly configured.");
            }

            ProjectilePool pool = GetOrCreatePool(parameters.ProjectilePrefabRuntime, parameters.MaxPoolSize);
            ProjectileRuntime projectile = pool.Get();

            Vector3 ownerForward = context.OwnerTransform.forward;
            if (ownerForward.sqrMagnitude <= 0.0001f)
            {
                ownerForward = Vector3.forward;
            }

            ownerForward.Normalize();

            Vector3 spawnPosition = ResolveSpawnPosition(context, parameters, ownerForward);
            LayerMask hitLayers = context.ResolveTargetLayers(data);
            Vector3 launchDirection = ResolveLaunchDirection(context, parameters, spawnPosition, ownerForward, hitLayers);

            projectile.Launch(
                spawnPosition,
                launchDirection,
                parameters.ProjectileSpeed,
                parameters.MaxLifeTimeSeconds,
                parameters.HitRadius,
                hitLayers,
                context.OwnerTransform,
                parameters.TargetHitVisualProfile,
                parameters.ImpactVfxPrefab,
                parameters.ImpactVfxDelaySeconds,
                parameters.ImpactVfxAutoReturnSeconds,
                parameters.ImpactSfx,
                parameters.ImpactSfxVolume,
                context.PooledVfxService,
                pool.Release);

            return UniTask.CompletedTask;
        }

        public override bool TryValidate(AbilityDataSO data, out string validationError)
        {
            if (!TryResolveParameters(data, out ProjectileParameters parameters))
            {
                validationError = "Projectile executor requires projectile mechanic config (ProjectileMechanicConfigSO).";
                return false;
            }

            if (parameters.ProjectilePrefabRuntime == null)
            {
                validationError = "Projectile prefab must include ProjectileRuntime.";
                return false;
            }

            if (!IsValid(parameters))
            {
                validationError = "Projectile speed, max lifetime and aim distance must be greater than 0. MaxPoolSize must be -1 or > 0.";
                return false;
            }

            if (data != null && data.TargetGroups == AbilityTargetGroups.None)
            {
                validationError = "Projectile target groups cannot be None.";
                return false;
            }

            validationError = string.Empty;
            return true;
        }

        private static bool TryResolveParameters(AbilityDataSO data, out ProjectileParameters parameters)
        {
            if (data != null && data.TryGetMechanicConfig(out ProjectileMechanicConfigSO module))
            {
                ProjectileRuntime prefabRuntime = GetProjectileRuntime(module.ProjectilePrefab);
                ResolveHitFeedbackModule(
                    data,
                    out HitVisualProfileSO targetHitVisualProfile,
                    out GameObject impactVfxPrefab,
                    out float impactVfxDelaySeconds,
                    out float impactVfxAutoReturnSeconds,
                    out AudioClip impactSfx,
                    out float impactSfxVolume);

                parameters = new ProjectileParameters(
                    prefabRuntime,
                    impactVfxPrefab,
                    impactVfxDelaySeconds,
                    impactVfxAutoReturnSeconds,
                    impactSfx,
                    impactSfxVolume,
                    module.MaxPoolSize,
                    module.ProjectileSpeed,
                    module.MaxLifeTimeSeconds,
                    module.HitRadius,
                    targetHitVisualProfile,
                    module.SpawnForwardOffset,
                    module.SpawnHorizontalOffset,
                    module.SpawnVerticalOffset,
                    module.AimMaxDistance,
                    module.AimRayVerticalOffset);
                return true;
            }

            parameters = default;
            return false;
        }

        private static void ResolveHitFeedbackModule(
            AbilityDataSO data,
            out HitVisualProfileSO targetHitVisualProfile,
            out GameObject impactVfxPrefab,
            out float impactVfxDelaySeconds,
            out float impactVfxAutoReturnSeconds,
            out AudioClip impactSfx,
            out float impactSfxVolume)
        {
            if (data != null && data.TryGetModule(out AbilityHitFeedbackModuleSO hitFeedbackModule))
            {
                targetHitVisualProfile = hitFeedbackModule.TargetHitVisualProfile;
                impactVfxPrefab = hitFeedbackModule.ImpactVfxPrefab;
                impactVfxDelaySeconds = hitFeedbackModule.ImpactVfxDelaySeconds;
                impactVfxAutoReturnSeconds = hitFeedbackModule.ImpactVfxAutoReturnSeconds;
                impactSfx = hitFeedbackModule.ImpactSfx;
                impactSfxVolume = hitFeedbackModule.ImpactSfxVolume;
                return;
            }

            targetHitVisualProfile = null;
            impactVfxPrefab = null;
            impactVfxDelaySeconds = 0f;
            impactVfxAutoReturnSeconds = 0f;
            impactSfx = null;
            impactSfxVolume = 1f;
        }

        private static Vector3 ResolveSpawnPosition(AbilityContext context, ProjectileParameters parameters, Vector3 ownerForward)
        {
            Transform ownerTransform = context.OwnerTransform;
            Vector3 ownerPosition = ownerTransform.position;
            Vector3 ownerRight = ownerTransform.right;
            Vector3 ownerUp = ownerTransform.up;

            return ownerPosition
                   + ownerForward * parameters.SpawnForwardOffset
                   + ownerRight * parameters.SpawnHorizontalOffset
                   + ownerUp * parameters.SpawnVerticalOffset;
        }

        private static Vector3 ResolveLaunchDirection(
            AbilityContext context,
            ProjectileParameters parameters,
            Vector3 spawnPosition,
            Vector3 fallbackForward,
            LayerMask hitLayers)
        {
            Transform ownerTransform = context.OwnerTransform;
            Vector3 rayOrigin = ownerTransform.position + ownerTransform.up * parameters.AimRayVerticalOffset;

            if (Physics.Raycast(
                    rayOrigin,
                    fallbackForward,
                    out RaycastHit hit,
                    parameters.AimMaxDistance,
                    hitLayers,
                    QueryTriggerInteraction.Ignore))
            {
                Vector3 directionToHit = hit.point - spawnPosition;
                if (directionToHit.sqrMagnitude > 0.0001f)
                {
                    return directionToHit.normalized;
                }
            }

            return fallbackForward;
        }

        private static ProjectileRuntime GetProjectileRuntime(GameObject projectilePrefab)
        {
            return projectilePrefab != null ? projectilePrefab.GetComponent<ProjectileRuntime>() : null;
        }

        private static bool IsValid(ProjectileParameters parameters)
        {
            bool hasValidPoolSize = parameters.MaxPoolSize == -1 || parameters.MaxPoolSize > 0;

            return parameters.ProjectilePrefabRuntime != null
                   && hasValidPoolSize
                   && parameters.ProjectileSpeed > 0f
                   && parameters.MaxLifeTimeSeconds > 0f
                   && parameters.AimMaxDistance > 0f;
        }

        private ProjectilePool GetOrCreatePool(ProjectileRuntime projectilePrefab, int maxPoolSize)
        {
            var key = (projectilePrefab.GetInstanceID(), maxPoolSize);
            if (_poolByKey.TryGetValue(key, out ProjectilePool pool))
            {
                return pool;
            }

            pool = new ProjectilePool(projectilePrefab, maxPoolSize);
            _poolByKey[key] = pool;
            return pool;
        }

        private readonly struct ProjectileParameters
        {
            public ProjectileParameters(
                ProjectileRuntime projectilePrefabRuntime,
                GameObject impactVfxPrefab,
                float impactVfxDelaySeconds,
                float impactVfxAutoReturnSeconds,
                AudioClip impactSfx,
                float impactSfxVolume,
                int maxPoolSize,
                float projectileSpeed,
                float maxLifeTimeSeconds,
                float hitRadius,
                HitVisualProfileSO targetHitVisualProfile,
                float spawnForwardOffset,
                float spawnHorizontalOffset,
                float spawnVerticalOffset,
                float aimMaxDistance,
                float aimRayVerticalOffset)
            {
                ProjectilePrefabRuntime = projectilePrefabRuntime;
                ImpactVfxPrefab = impactVfxPrefab;
                ImpactVfxDelaySeconds = impactVfxDelaySeconds;
                ImpactVfxAutoReturnSeconds = impactVfxAutoReturnSeconds;
                ImpactSfx = impactSfx;
                ImpactSfxVolume = impactSfxVolume;
                MaxPoolSize = maxPoolSize;
                ProjectileSpeed = projectileSpeed;
                MaxLifeTimeSeconds = maxLifeTimeSeconds;
                HitRadius = hitRadius;
                TargetHitVisualProfile = targetHitVisualProfile;
                SpawnForwardOffset = spawnForwardOffset;
                SpawnHorizontalOffset = spawnHorizontalOffset;
                SpawnVerticalOffset = spawnVerticalOffset;
                AimMaxDistance = aimMaxDistance;
                AimRayVerticalOffset = aimRayVerticalOffset;
            }

            public ProjectileRuntime ProjectilePrefabRuntime { get; }
            public GameObject ImpactVfxPrefab { get; }
            public float ImpactVfxDelaySeconds { get; }
            public float ImpactVfxAutoReturnSeconds { get; }
            public AudioClip ImpactSfx { get; }
            public float ImpactSfxVolume { get; }
            public int MaxPoolSize { get; }
            public float ProjectileSpeed { get; }
            public float MaxLifeTimeSeconds { get; }
            public float HitRadius { get; }
            public HitVisualProfileSO TargetHitVisualProfile { get; }
            public float SpawnForwardOffset { get; }
            public float SpawnHorizontalOffset { get; }
            public float SpawnVerticalOffset { get; }
            public float AimMaxDistance { get; }
            public float AimRayVerticalOffset { get; }
        }
    }
}
