using System;
using System.Threading;
using CaseStudy.Feature.AbilitySystem.Contracts;
using CaseStudy.Feature.AbilitySystem.Data;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CaseStudy.Feature.AbilitySystem.Abilities
{
    public sealed class AoeAbility : BaseAbility
    {
        private AoeAbilityDataSO _aoeData;
        private Collider[] _overlapBuffer;

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

            int hitCount = Physics.OverlapSphereNonAlloc(
                center,
                _aoeData.Radius,
                _overlapBuffer,
                _aoeData.AffectedLayers,
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
                //todo: pool
                UnityEngine.Object.Instantiate(_aoeData.AoeVfxPrefab, center, Quaternion.identity);
            }

            return UniTask.CompletedTask;
        }
    }
}