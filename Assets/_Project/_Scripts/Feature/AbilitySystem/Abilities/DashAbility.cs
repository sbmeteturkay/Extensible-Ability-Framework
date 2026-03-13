using System;
using System.Threading;
using CaseStudy.Feature.AbilitySystem.Data;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CaseStudy.Feature.AbilitySystem.Abilities
{
    public sealed class DashAbility : BaseAbility
    {
        private DashAbilityDataSO _dashData;
        private bool _isDashing;

        public override void Initialize(Domain.AbilityContext context, AbilityDataSO data)
        {
            base.Initialize(context, data);
            _dashData = data as DashAbilityDataSO;
        }

        public override bool CanExecute()
        {
            return base.CanExecute()
                   && !_isDashing
                   && _dashData != null
                   && _dashData.DashDistance > 0f
                   && _dashData.DashDurationSeconds > 0f;
        }

        public override async UniTask ExecuteAsync(CancellationToken cancellationToken)
        {
            if (!CanExecute())
            {
                throw new InvalidOperationException("Dash ability is not ready.");
            }

            _isDashing = true;

            try
            {
                Vector3 direction = Context.OwnerTransform.forward;
                direction.y = 0f;

                if (direction.sqrMagnitude <= 0.0001f)
                {
                    direction = Context.OwnerTransform.right;
                    direction.y = 0f;
                }

                direction.Normalize();

                Vector3 startPosition = Context.OwnerRigidbody.position;
                Vector3 endPosition = startPosition + direction * _dashData.DashDistance;
                float duration = Mathf.Max(0.01f, _dashData.DashDurationSeconds);
                float elapsed = 0f;

                while (elapsed < duration)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    float normalizedTime = Mathf.Clamp01(elapsed / duration);
                    float curveTime = Mathf.Clamp01(_dashData.SpeedCurve.Evaluate(normalizedTime));
                    Vector3 nextPosition = Vector3.LerpUnclamped(startPosition, endPosition, curveTime);

                    Context.OwnerRigidbody.MovePosition(nextPosition);

                    await UniTask.WaitForFixedUpdate(cancellationToken);
                    elapsed += Time.fixedDeltaTime;
                }

                Context.OwnerRigidbody.MovePosition(endPosition);
            }
            finally
            {
                _isDashing = false;
            }
        }
    }
}
