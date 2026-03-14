using System;
using System.Threading;
using CaseStudy.Feature.AbilitySystem.Contracts;
using CaseStudy.Feature.AbilitySystem.Domain;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CaseStudy.Feature.AbilitySystem.Data
{
    [CreateAssetMenu(fileName = "SO_Executor_Aoe", menuName = "Ability/Executors/AOE Executor")]
    public sealed class AoeExecutorSO : AbilityExecutorSO
    {
        private Collider[] _overlapBuffer = Array.Empty<Collider>();

        public override bool CanExecute(AbilityContext context, AbilityDataSO data)
        {
            return base.CanExecute(context, data)
                   && TryResolveParameters(data, out AoeParameters parameters)
                   && IsValid(parameters)
                   && data.TargetGroups != AbilityTargetGroups.None;
        }

        public override UniTask ExecuteAsync(AbilityContext context, AbilityDataSO data, CancellationToken cancellationToken)
        {
            if (!CanExecute(context, data) || !TryResolveParameters(data, out AoeParameters parameters))
            {
                throw new InvalidOperationException("AoeExecutorSO is not properly configured.");
            }

            EnsureBufferSize(parameters.MaxTargets);

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

                Component receiverComponent = hitCollider.GetComponent(typeof(IAoeEffectReceiver)) as Component;
                if (receiverComponent is IAoeEffectReceiver effectReceiver)
                {
                    effectReceiver.ApplyAoeEffect(parameters.EffectDurationSeconds);
                }
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
                    SpawnAoeVfxAsync(parameters.AoeVfxPrefab, center, parameters.AoeVfxDelaySeconds).Forget();
                }
            }

            return UniTask.CompletedTask;
        }

        public override bool TryValidate(AbilityDataSO data, out string validationError)
        {
            if (!TryResolveParameters(data, out AoeParameters parameters))
            {
                validationError = "AOE executor requires AoeAbilityModuleSO.";
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
            if (data != null && data.TryGetModule(out AoeAbilityModuleSO module))
            {
                parameters = new AoeParameters(
                    module.Radius,
                    module.EffectDurationSeconds,
                    module.MaxTargets,
                    module.AoeVfxPrefab,
                    module.AoeVfxDelaySeconds,
                    module.AoeVfxAutoReturnSeconds);
                return true;
            }

            parameters = default;
            return false;
        }

        private static bool IsValid(AoeParameters parameters)
        {
            return parameters.Radius > 0f && parameters.MaxTargets > 0;
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

        private static async UniTaskVoid SpawnAoeVfxAsync(GameObject vfxPrefab, Vector3 center, float delaySeconds)
        {
            if (delaySeconds > 0f)
            {
                int delayMilliseconds = Mathf.CeilToInt(delaySeconds * 1000f);
                if (delayMilliseconds > 0)
                {
                    await UniTask.Delay(delayMilliseconds, DelayType.DeltaTime, PlayerLoopTiming.Update, CancellationToken.None);
                }
            }

            UnityEngine.Object.Instantiate(vfxPrefab, center, Quaternion.identity);
        }

        private readonly struct AoeParameters
        {
            public AoeParameters(
                float radius,
                float effectDurationSeconds,
                int maxTargets,
                GameObject aoeVfxPrefab,
                float aoeVfxDelaySeconds,
                float aoeVfxAutoReturnSeconds)
            {
                Radius = radius;
                EffectDurationSeconds = effectDurationSeconds;
                MaxTargets = maxTargets;
                AoeVfxPrefab = aoeVfxPrefab;
                AoeVfxDelaySeconds = aoeVfxDelaySeconds;
                AoeVfxAutoReturnSeconds = aoeVfxAutoReturnSeconds;
            }

            public float Radius { get; }

            public float EffectDurationSeconds { get; }

            public int MaxTargets { get; }

            public GameObject AoeVfxPrefab { get; }

            public float AoeVfxDelaySeconds { get; }

            public float AoeVfxAutoReturnSeconds { get; }
        }
    }
}
