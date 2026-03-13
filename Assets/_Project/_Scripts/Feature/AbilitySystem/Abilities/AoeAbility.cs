using System;
using System.Threading;
using CaseStudy.Feature.AbilitySystem.Contracts;
using CaseStudy.Feature.AbilitySystem.Data;
using CaseStudy.Shared.Vfx.Interfaces;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CaseStudy.Feature.AbilitySystem.Abilities
{
    public sealed class AoeAbility : BaseAbility
    {
        private readonly IPooledVfxService _pooledVfxService;

        private AoeAbilityDataSO _aoeData;
        private Collider[] _overlapBuffer;

        public AoeAbility(IPooledVfxService pooledVfxService)
        {
            _pooledVfxService = pooledVfxService;
        }

        public override void Initialize(Domain.AbilityContext context, AbilityDataSO data)
        {
            base.Initialize(context, data);
            _aoeData = data as AoeAbilityDataSO;

            int bufferSize = _aoeData != null ? Mathf.Max(1, _aoeData.MaxTargets) : 1;
            _overlapBuffer = new Collider[bufferSize];
        }

        public override bool CanExecute()
        {
            return base.CanExecute()
                   && _aoeData != null
                   && _aoeData.Radius > 0f;
        }

        public override UniTask ExecuteAsync(CancellationToken cancellationToken)
        {
            if (!CanExecute())
            {
                throw new InvalidOperationException("AOE ability is not properly configured.");
            }

            Vector3 center = Context.OwnerTransform.position;
            LayerMask targetLayers = Context.ResolveTargetLayers(_aoeData);

            int hitCount = Physics.OverlapSphereNonAlloc(
                center,
                _aoeData.Radius,
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
                    effectReceiver.ApplyAoeEffect(_aoeData.EffectDurationSeconds);
                }
            }

            if (_aoeData.AoeVfxPrefab != null)
            {
                if (_pooledVfxService != null)
                {
                    _pooledVfxService.Spawn(
                        _aoeData.AoeVfxPrefab,
                        center,
                        Quaternion.identity,
                        _aoeData.AoeVfxDelaySeconds,
                        _aoeData.AoeVfxAutoReturnSeconds);
                }
                else
                {
                    SpawnAoeVfxAsync(_aoeData.AoeVfxPrefab, center, _aoeData.AoeVfxDelaySeconds).Forget();
                }
            }

            return UniTask.CompletedTask;
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
    }
}
