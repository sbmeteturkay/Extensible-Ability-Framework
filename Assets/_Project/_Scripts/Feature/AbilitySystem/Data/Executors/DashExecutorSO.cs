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
        private const float MIN_DIRECTION_SQR = 0.0001f;
        private const float MIN_DURATION_SECONDS = 0.01f;
        private const float COLLISION_SKIN = 0.02f;

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

            if (direction.sqrMagnitude <= MIN_DIRECTION_SQR)
            {
                direction = context.OwnerTransform.right;
                direction.y = 0f;
            }

            if (direction.sqrMagnitude <= MIN_DIRECTION_SQR)
            {
                return;
            }

            direction.Normalize();

            float duration = Mathf.Max(MIN_DURATION_SECONDS, parameters.DurationSeconds);
            AnimationCurve speedCurve = parameters.SpeedCurve != null
                ? parameters.SpeedCurve
                : AnimationCurve.Linear(0f, 0f, 1f, 1f);

            Vector3 dashStart = context.OwnerRigidbody.position;
            float elapsed = 0f;
            bool blocked = false;
            RaycastHit blockHit = default;

            while (elapsed < duration)
            {
                cancellationToken.ThrowIfCancellationRequested();

                float normalizedTime = Mathf.Clamp01(elapsed / duration);
                float curveDistanceFactor = Mathf.Clamp01(speedCurve.Evaluate(normalizedTime));
                Vector3 desiredPosition = dashStart + direction * (parameters.Distance * curveDistanceFactor);

                Vector3 currentPosition = context.OwnerRigidbody.position;
                Vector3 desiredDelta = desiredPosition - currentPosition;

                MoveStep(context.OwnerRigidbody, desiredDelta, out blocked, out blockHit);
                if (blocked)
                {
                    if (parameters.EnableDebugTelemetry)
                    {
                        LogDashBlocked(context.OwnerRigidbody, blockHit, elapsed, duration);
                    }
                    break;
                }

                await UniTask.WaitForFixedUpdate(cancellationToken);
                elapsed += Time.fixedDeltaTime;
            }

            if (!blocked)
            {
                Vector3 finalTarget = dashStart + direction * parameters.Distance;
                Vector3 finalDelta = finalTarget - context.OwnerRigidbody.position;
                MoveStep(context.OwnerRigidbody, finalDelta, out blocked, out blockHit);

                if (blocked && parameters.EnableDebugTelemetry)
                {
                    LogDashBlocked(context.OwnerRigidbody, blockHit, duration, duration);
                }
                else if (parameters.EnableDebugTelemetry)
                {
                    LogDashCompleted(context.OwnerRigidbody, dashStart, finalTarget);
                }
            }
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

        private static void MoveStep(Rigidbody ownerRigidbody, Vector3 desiredDelta, out bool blocked, out RaycastHit hit)
        {
            blocked = false;
            hit = default;

            float distance = desiredDelta.magnitude;
            if (distance <= 0.0001f)
            {
                return;
            }

            Vector3 direction = desiredDelta / distance;

            if (ownerRigidbody.SweepTest(direction, out hit, distance, QueryTriggerInteraction.Ignore))
            {
                float allowedDistance = Mathf.Max(0f, hit.distance - COLLISION_SKIN);
                Vector3 clampedDelta = direction * allowedDistance;
                ownerRigidbody.MovePosition(ownerRigidbody.position + clampedDelta);
                blocked = true;
                return;
            }

            ownerRigidbody.MovePosition(ownerRigidbody.position + desiredDelta);
        }

        private static void LogDashBlocked(Rigidbody ownerRigidbody, RaycastHit hit, float elapsed, float duration)
        {
            string colliderName = hit.collider != null ? hit.collider.name : "<null>";
            int colliderLayer = hit.collider != null ? hit.collider.gameObject.layer : -1;
            string layerName = colliderLayer >= 0 ? LayerMask.LayerToName(colliderLayer) : "None";

            Debug.Log(
                $"Dash blocked at t={elapsed:0.000}/{duration:0.000}s | hit='{colliderName}' layer={colliderLayer} ({layerName}) distance={hit.distance:0.###} | rbPos={ownerRigidbody.position}");
        }

        private static void LogDashCompleted(Rigidbody ownerRigidbody, Vector3 start, Vector3 intendedEnd)
        {
            Debug.Log(
                $"Dash completed | start={start} intendedEnd={intendedEnd} final={ownerRigidbody.position}");
        }

        private static bool TryResolveParameters(AbilityDataSO data, out DashParameters parameters)
        {
            if (data != null && data.TryGetMechanicConfig(out DashMechanicConfigSO module))
            {
                parameters = new DashParameters(
                    module.DashDistance,
                    module.DashDurationSeconds,
                    module.SpeedCurve,
                    module.EnableDebugTelemetry);
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
            public DashParameters(float distance, float durationSeconds, AnimationCurve speedCurve, bool enableDebugTelemetry)
            {
                Distance = distance;
                DurationSeconds = durationSeconds;
                SpeedCurve = speedCurve;
                EnableDebugTelemetry = enableDebugTelemetry;
            }

            public float Distance { get; }
            public float DurationSeconds { get; }
            public AnimationCurve SpeedCurve { get; }
            public bool EnableDebugTelemetry { get; }
        }
    }
}
