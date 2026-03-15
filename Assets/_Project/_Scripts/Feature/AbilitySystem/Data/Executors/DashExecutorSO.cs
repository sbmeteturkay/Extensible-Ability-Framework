using System;
using System.Threading;
using CaseStudy.Feature.AbilitySystem.Domain;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CaseStudy.Feature.AbilitySystem.Data
{
    [CreateAssetMenu(fileName = "SO_Executor_Dash", menuName = "Ability/Executors/Dash Executor")]
    public sealed class DashExecutorSO : AbilityExecutorSO
    {
        public override Type RequiredMechanicConfigType => typeof(DashMechanicConfigSO);
        public override bool CanExecute(AbilityContext context, AbilityDataSO data)
        {
            return base.CanExecute(context, data)
                   && context.OwnerRigidbody != null
                   && context.OwnerTransform != null
                   && TryResolveParameters(data, out DashParameters parameters)
                   && IsValid(parameters);
        }

        public override async UniTask ExecuteAsync(AbilityContext context, AbilityDataSO data, CancellationToken cancellationToken)
        {
            if (!CanExecute(context, data) || !TryResolveParameters(data, out DashParameters parameters))
            {
                throw new InvalidOperationException("DashExecutorSO is not properly configured.");
            }

            Vector3 direction = context.OwnerTransform.forward;
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = context.OwnerTransform.right;
                direction.y = 0f;
            }

            direction.Normalize();

            Vector3 startPosition = context.OwnerRigidbody.position;
            Vector3 endPosition = startPosition + direction * parameters.Distance;
            float duration = Mathf.Max(0.01f, parameters.DurationSeconds);
            AnimationCurve speedCurve = parameters.SpeedCurve != null
                ? parameters.SpeedCurve
                : AnimationCurve.Linear(0f, 1f, 1f, 1f);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                cancellationToken.ThrowIfCancellationRequested();

                float normalizedTime = Mathf.Clamp01(elapsed / duration);
                float curveTime = Mathf.Clamp01(speedCurve.Evaluate(normalizedTime));
                Vector3 nextPosition = Vector3.LerpUnclamped(startPosition, endPosition, curveTime);

                context.OwnerRigidbody.MovePosition(nextPosition);

                await UniTask.WaitForFixedUpdate(cancellationToken);
                elapsed += Time.fixedDeltaTime;
            }

            context.OwnerRigidbody.MovePosition(endPosition);
        }

        public override bool TryValidate(AbilityDataSO data, out string validationError)
        {
            if (!TryResolveParameters(data, out DashParameters parameters))
            {
                validationError = "Dash executor requires Dash mechanic config (DashMechanicConfigSO).";
                return false;
            }

            if (!IsValid(parameters))
            {
                validationError = "Dash distance and duration must be greater than 0.";
                return false;
            }

            validationError = string.Empty;
            return true;
        }

        private static bool TryResolveParameters(AbilityDataSO data, out DashParameters parameters)
        {
            if (data != null && data.TryGetMechanicConfig(out DashMechanicConfigSO module))
            {
                parameters = new DashParameters(module.DashDistance, module.DashDurationSeconds, module.SpeedCurve);
                return true;
            }

            parameters = default;
            return false;
        }

        private static bool IsValid(DashParameters parameters)
        {
            return parameters.Distance > 0f && parameters.DurationSeconds > 0f;
        }

        private readonly struct DashParameters
        {
            public DashParameters(float distance, float durationSeconds, AnimationCurve speedCurve)
            {
                Distance = distance;
                DurationSeconds = durationSeconds;
                SpeedCurve = speedCurve;
            }

            public float Distance { get; }

            public float DurationSeconds { get; }

            public AnimationCurve SpeedCurve { get; }
        }
    }
}






