using System;
using System.Threading;
using CaseStudy.Feature.AbilitySystem.Contracts;
using CaseStudy.Feature.AbilitySystem.Domain;
using CaseStudy.Shared.Vfx.Interfaces;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CaseStudy.Feature.AbilitySystem.Data
{
    [CreateAssetMenu(fileName = "SO_Executor_Aoe", menuName = "Ability/Executors/AOE Executor")]
    public sealed class AoeExecutorSO : AbilityExecutorSO
    {
        public override Type RequiredMechanicConfigType => typeof(AoeMechanicConfigSO);
        private Collider[] _overlapBuffer = Array.Empty<Collider>();

        public override bool CanExecute(AbilityContext context, AbilityDataSO data)
        {
            return base.CanExecute(context, data)
                   && TryResolveParameters(data, out AoeParameters parameters)
                   && IsValid(parameters)
                   && data.TargetGroups != AbilityTargetGroups.None;
        }

        public override async UniTask ExecuteAsync(AbilityContext context, AbilityDataSO data, CancellationToken cancellationToken)
        {
            if (!CanExecute(context, data) || !TryResolveParameters(data, out AoeParameters parameters))
            {
                throw new InvalidOperationException("AoeExecutorSO is not properly configured.");
            }

            EnsureBufferSize(parameters.MaxTargets);

            if (parameters.HitDelaySeconds > 0f)
            {
                int delayMilliseconds = Mathf.CeilToInt(parameters.HitDelaySeconds * 1000f);
                if (delayMilliseconds > 0)
                {
                    await UniTask.Delay(delayMilliseconds, DelayType.DeltaTime, PlayerLoopTiming.Update, cancellationToken);
                }
            }

            Vector3 center = context.OwnerTransform.position;
            LayerMask targetLayers = context.ResolveTargetLayers(data);

            int hitCount = Physics.OverlapSphereNonAlloc(
                center,
                parameters.Radius,
                _overlapBuffer,
                targetLayers,
                QueryTriggerInteraction.Collide);

            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider = _overlapBuffer[i];
                if (hitCollider == null)
                {
                    continue;
                }

                Component effectComponent = hitCollider.GetComponent(typeof(IAoeEffectReceiver)) as Component;
                if (effectComponent is IAoeEffectReceiver effectReceiver)
                {
                    effectReceiver.ApplyAoeEffect(parameters.EffectDurationSeconds);
                }

                TryApplyHitVisual(hitCollider, parameters.TargetHitVisualProfile);
                TrySpawnTargetHitVfx(context.PooledVfxService, hitCollider, parameters.TargetHitVisualProfile);
            }

            if (parameters.AoeVfxPrefab != null)
            {
                if (context.PooledVfxService != null)
                {
                    context.PooledVfxService.Spawn(
                        parameters.AoeVfxPrefab,
                        center,
                        Quaternion.identity,
                        parameters.AoeVfxDelaySeconds,
                        parameters.AoeVfxAutoReturnSeconds);
                }
                else
                {
                    Debug.LogWarning("AoeExecutorSO: IPooledVfxService is missing. Skipping AOE VFX spawn.");
                }
            }
        }

        public override bool TryValidate(AbilityDataSO data, out string validationError)
        {
            if (!TryResolveParameters(data, out AoeParameters parameters))
            {
                validationError = "AOE executor requires AOE mechanic config (AoeMechanicConfigSO).";
                return false;
            }

            if (!IsValid(parameters))
            {
                validationError = "AOE radius/max targets must be greater than 0.";
                return false;
            }

            if (data != null && data.TargetGroups == AbilityTargetGroups.None)
            {
                validationError = "AOE target groups cannot be None.";
                return false;
            }

            validationError = string.Empty;
            return true;
        }

        private static bool TryResolveParameters(AbilityDataSO data, out AoeParameters parameters)
        {
            if (data != null && data.TryGetMechanicConfig(out AoeMechanicConfigSO module))
            {
                HitVisualProfileSO targetHitVisualProfile = ResolveTargetHitVisualProfile(data);

                parameters = new AoeParameters(
                    module.Radius,
                    module.HitDelaySeconds,
                    module.EffectDurationSeconds,
                    module.MaxTargets,
                    module.AoeVfxPrefab,
                    module.AoeVfxDelaySeconds,
                    module.AoeVfxAutoReturnSeconds,
                    targetHitVisualProfile);
                return true;
            }

            parameters = default;
            return false;
        }

        private static HitVisualProfileSO ResolveTargetHitVisualProfile(AbilityDataSO data)
        {
            return data != null && data.TryGetModule(out AbilityHitFeedbackModuleSO hitFeedbackModule)
                ? hitFeedbackModule.TargetHitVisualProfile
                : null;
        }

        private static bool IsValid(AoeParameters parameters)
        {
            return parameters.Radius > 0f && parameters.MaxTargets > 0;
        }

        private static void TryApplyHitVisual(Collider hitCollider, HitVisualProfileSO visualProfile)
        {
            if (hitCollider == null || visualProfile == null)
            {
                return;
            }

            Component visualComponent = hitCollider.GetComponent(typeof(IAbilityHitVisualReceiver)) as Component;
            if (visualComponent == null)
            {
                visualComponent = hitCollider.GetComponentInParent(typeof(IAbilityHitVisualReceiver)) as Component;
            }

            if (visualComponent is IAbilityHitVisualReceiver visualReceiver)
            {
                visualReceiver.ApplyHitVisual(visualProfile);
            }
        }

        private static void TrySpawnTargetHitVfx(
            IPooledVfxService pooledVfxService,
            Collider hitCollider,
            HitVisualProfileSO visualProfile)
        {
            if (pooledVfxService == null || hitCollider == null || visualProfile == null || visualProfile.HitVfxPrefab == null)
            {
                return;
            }

            pooledVfxService.Spawn(
                visualProfile.HitVfxPrefab,
                hitCollider.bounds.center,
                Quaternion.identity,
                visualProfile.HitVfxDelaySeconds,
                visualProfile.HitVfxAutoReturnSeconds);
        }

        private void EnsureBufferSize(int requiredSize)
        {
            int clampedSize = Mathf.Max(1, requiredSize);
            if (_overlapBuffer.Length >= clampedSize)
            {
                return;
            }

            _overlapBuffer = new Collider[clampedSize];
        }

        private readonly struct AoeParameters
        {
            public AoeParameters(
                float radius,
                float hitDelaySeconds,
                float effectDurationSeconds,
                int maxTargets,
                GameObject aoeVfxPrefab,
                float aoeVfxDelaySeconds,
                float aoeVfxAutoReturnSeconds,
                HitVisualProfileSO targetHitVisualProfile)
            {
                Radius = radius;
                HitDelaySeconds = hitDelaySeconds;
                EffectDurationSeconds = effectDurationSeconds;
                MaxTargets = maxTargets;
                AoeVfxPrefab = aoeVfxPrefab;
                AoeVfxDelaySeconds = aoeVfxDelaySeconds;
                AoeVfxAutoReturnSeconds = aoeVfxAutoReturnSeconds;
                TargetHitVisualProfile = targetHitVisualProfile;
            }

            public float Radius { get; }

            public float HitDelaySeconds { get; }

            public float EffectDurationSeconds { get; }

            public int MaxTargets { get; }

            public GameObject AoeVfxPrefab { get; }

            public float AoeVfxDelaySeconds { get; }

            public float AoeVfxAutoReturnSeconds { get; }

            public HitVisualProfileSO TargetHitVisualProfile { get; }
        }
    }
}
