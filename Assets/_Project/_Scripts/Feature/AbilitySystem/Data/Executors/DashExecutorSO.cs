using System;
using System.Collections.Generic;
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
        private const float CONTACT_DISTANCE_EPSILON = 0.001f;
        private const float FORWARD_BLOCK_DOT_THRESHOLD = 0.05f;
        private const int LANDING_OVERLAP_BUFFER_SIZE = 32;
        private const int MOVEMENT_CAST_BUFFER_SIZE = 16;

        private static readonly Collider[] LandingOverlapBuffer = new Collider[LANDING_OVERLAP_BUFFER_SIZE];
        private static readonly RaycastHit[] MovementCastBuffer = new RaycastHit[MOVEMENT_CAST_BUFFER_SIZE];

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

            if (!TryBuildColliderCache(context.OwnerRigidbody, out DashColliderCache colliderCache))
            {
                if (parameters.EnableDebugTelemetry)
                {
                    Debug.LogWarning("DashExecutorSO: no valid non-trigger collider found. Dash collision checks may be inaccurate.");
                }

                colliderCache = DashColliderCache.Empty;
            }

            float duration = Mathf.Max(MIN_DURATION_SECONDS, parameters.DurationSeconds);
            AnimationCurve speedCurve = parameters.SpeedCurve != null
                ? parameters.SpeedCurve
                : AnimationCurve.Linear(0f, 0f, 1f, 1f);

            Vector3 dashStart = context.OwnerRigidbody.position;
            Vector3 finalTarget = dashStart + direction * parameters.Distance;
            LayerMask collisionMask = ResolveDashCollisionMask(context, data);
            bool usePhaseMode = parameters.AllowPhaseThroughObstaclesIfLandingIsValid
                                && IsLandingPositionValid(context.OwnerRigidbody, finalTarget, colliderCache, collisionMask);

            if (parameters.AllowPhaseThroughObstaclesIfLandingIsValid
                && !usePhaseMode
                && parameters.EnableDebugTelemetry)
            {
                Debug.Log("Dash phase mode requested but landing is not valid. Falling back to collision mode.");
            }

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

                if (usePhaseMode)
                {
                    MoveStepIgnoringCollision(context.OwnerRigidbody, desiredDelta);
                }
                else
                {
                    MoveStepWithCollision(context.OwnerRigidbody, desiredDelta, colliderCache, collisionMask, out blocked, out blockHit);
                    if (blocked)
                    {
                        if (parameters.EnableDebugTelemetry)
                        {
                            LogDashBlocked(context.OwnerRigidbody, blockHit, elapsed, duration);
                        }

                        break;
                    }
                }

                await UniTask.WaitForFixedUpdate(cancellationToken);
                elapsed += Time.fixedDeltaTime;
            }

            if (!blocked)
            {
                Vector3 finalDelta = finalTarget - context.OwnerRigidbody.position;
                if (usePhaseMode)
                {
                    MoveStepIgnoringCollision(context.OwnerRigidbody, finalDelta);
                    if (parameters.EnableDebugTelemetry)
                    {
                        Debug.Log($"Dash phased through obstacles and landed at {context.OwnerRigidbody.position}");
                    }
                }
                else
                {
                    MoveStepWithCollision(context.OwnerRigidbody, finalDelta, colliderCache, collisionMask, out blocked, out blockHit);
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

        private static void MoveStepWithCollision(
            Rigidbody ownerRigidbody,
            Vector3 desiredDelta,
            DashColliderCache colliderCache,
            LayerMask collisionMask,
            out bool blocked,
            out RaycastHit hit)
        {
            blocked = false;
            hit = default;

            float distance = desiredDelta.magnitude;
            if (distance <= 0.0001f)
            {
                return;
            }

            Vector3 direction = desiredDelta / distance;

            if (TryGetClosestBlockingHit(ownerRigidbody, direction, distance, colliderCache, collisionMask, out hit))
            {
                float allowedDistance = Mathf.Max(0f, hit.distance - COLLISION_SKIN);
                Vector3 clampedDelta = direction * allowedDistance;
                ownerRigidbody.MovePosition(ownerRigidbody.position + clampedDelta);
                blocked = true;
                return;
            }

            ownerRigidbody.MovePosition(ownerRigidbody.position + desiredDelta);
        }

        private static bool TryGetClosestBlockingHit(
            Rigidbody ownerRigidbody,
            Vector3 direction,
            float distance,
            DashColliderCache colliderCache,
            LayerMask collisionMask,
            out RaycastHit closestHit)
        {
            closestHit = default;

            if (ownerRigidbody == null || distance <= 0f || direction.sqrMagnitude <= 0f)
            {
                return false;
            }

            if (!colliderCache.HasPrimaryCollider)
            {
                return ownerRigidbody.SweepTest(direction, out closestHit, distance, QueryTriggerInteraction.Ignore);
            }

            Quaternion rotation = ownerRigidbody.rotation;
            int hitCount;

            if (colliderCache.PrimaryCollider is CapsuleCollider capsule)
            {
                Vector3 worldCenter = ownerRigidbody.position + rotation * Vector3.Scale(capsule.center, colliderCache.OwnerScale);
                Vector3 axis = GetWorldAxis(rotation, capsule.direction);
                float axisScale = GetAxisScale(colliderCache.AbsoluteScale, capsule.direction);
                float radiusScale = GetPerpendicularMaxScale(colliderCache.AbsoluteScale, capsule.direction);
                float scaledRadius = capsule.radius * radiusScale;
                float scaledHeight = capsule.height * axisScale;
                float halfLine = Mathf.Max(0f, (scaledHeight * 0.5f) - scaledRadius);
                Vector3 point0 = worldCenter + axis * halfLine;
                Vector3 point1 = worldCenter - axis * halfLine;

                hitCount = Physics.CapsuleCastNonAlloc(
                    point0,
                    point1,
                    scaledRadius,
                    direction,
                    MovementCastBuffer,
                    distance + COLLISION_SKIN,
                    collisionMask,
                    QueryTriggerInteraction.Ignore);
            }
            else if (colliderCache.PrimaryCollider is SphereCollider sphere)
            {
                Vector3 worldCenter = ownerRigidbody.position + rotation * Vector3.Scale(sphere.center, colliderCache.OwnerScale);
                float scaledRadius = sphere.radius * Mathf.Max(colliderCache.AbsoluteScale.x, colliderCache.AbsoluteScale.y, colliderCache.AbsoluteScale.z);

                hitCount = Physics.SphereCastNonAlloc(
                    worldCenter,
                    scaledRadius,
                    direction,
                    MovementCastBuffer,
                    distance + COLLISION_SKIN,
                    collisionMask,
                    QueryTriggerInteraction.Ignore);
            }
            else if (colliderCache.PrimaryCollider is BoxCollider box)
            {
                Vector3 worldCenter = ownerRigidbody.position + rotation * Vector3.Scale(box.center, colliderCache.OwnerScale);
                Vector3 halfExtents = Vector3.Scale(box.size * 0.5f, colliderCache.AbsoluteScale);

                hitCount = Physics.BoxCastNonAlloc(
                    worldCenter,
                    halfExtents,
                    direction,
                    MovementCastBuffer,
                    rotation,
                    distance + COLLISION_SKIN,
                    collisionMask,
                    QueryTriggerInteraction.Ignore);
            }
            else
            {
                return ownerRigidbody.SweepTest(direction, out closestHit, distance, QueryTriggerInteraction.Ignore);
            }

            float closestDistance = float.MaxValue;
            bool hasBlockingHit = false;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit candidate = MovementCastBuffer[i];
                Collider candidateCollider = candidate.collider;
                if (candidateCollider == null)
                {
                    continue;
                }

                if (IsSelfCollider(candidateCollider, colliderCache.OwnerColliders))
                {
                    continue;
                }

                if (Physics.GetIgnoreLayerCollision(colliderCache.OwnerLayer, candidateCollider.gameObject.layer))
                {
                    continue;
                }

                if (candidate.distance <= CONTACT_DISTANCE_EPSILON)
                {
                    float forwardBlockDot = Vector3.Dot(-candidate.normal, direction);
                    if (forwardBlockDot <= FORWARD_BLOCK_DOT_THRESHOLD)
                    {
                        continue;
                    }
                }

                if (candidate.distance < closestDistance)
                {
                    closestDistance = candidate.distance;
                    closestHit = candidate;
                    hasBlockingHit = true;
                }
            }

            return hasBlockingHit;
        }

        private static void MoveStepIgnoringCollision(Rigidbody ownerRigidbody, Vector3 desiredDelta)
        {
            if (desiredDelta.sqrMagnitude <= 0.00000001f)
            {
                return;
            }

            ownerRigidbody.MovePosition(ownerRigidbody.position + desiredDelta);
        }

        private static bool IsLandingPositionValid(Rigidbody ownerRigidbody, Vector3 targetPosition, DashColliderCache colliderCache, LayerMask collisionMask)
        {
            if (ownerRigidbody == null || !colliderCache.HasPrimaryCollider)
            {
                return false;
            }

            Quaternion rotation = ownerRigidbody.rotation;
            int overlapCount = 0;

            if (colliderCache.PrimaryCollider is CapsuleCollider capsule)
            {
                Vector3 worldCenter = targetPosition + rotation * Vector3.Scale(capsule.center, colliderCache.OwnerScale);
                Vector3 axis = GetWorldAxis(rotation, capsule.direction);

                float axisScale = GetAxisScale(colliderCache.AbsoluteScale, capsule.direction);
                float radiusScale = GetPerpendicularMaxScale(colliderCache.AbsoluteScale, capsule.direction);
                float scaledRadius = capsule.radius * radiusScale;
                float scaledHeight = capsule.height * axisScale;
                float halfLine = Mathf.Max(0f, (scaledHeight * 0.5f) - scaledRadius);

                Vector3 point0 = worldCenter + axis * halfLine;
                Vector3 point1 = worldCenter - axis * halfLine;

                overlapCount = Physics.OverlapCapsuleNonAlloc(point0, point1, scaledRadius, LandingOverlapBuffer, collisionMask, QueryTriggerInteraction.Ignore);
            }
            else if (colliderCache.PrimaryCollider is SphereCollider sphere)
            {
                Vector3 worldCenter = targetPosition + rotation * Vector3.Scale(sphere.center, colliderCache.OwnerScale);
                float scaledRadius = sphere.radius * Mathf.Max(colliderCache.AbsoluteScale.x, colliderCache.AbsoluteScale.y, colliderCache.AbsoluteScale.z);

                overlapCount = Physics.OverlapSphereNonAlloc(worldCenter, scaledRadius, LandingOverlapBuffer, collisionMask, QueryTriggerInteraction.Ignore);
            }
            else if (colliderCache.PrimaryCollider is BoxCollider box)
            {
                Vector3 worldCenter = targetPosition + rotation * Vector3.Scale(box.center, colliderCache.OwnerScale);
                Vector3 halfExtents = Vector3.Scale(box.size * 0.5f, colliderCache.AbsoluteScale);

                overlapCount = Physics.OverlapBoxNonAlloc(worldCenter, halfExtents, LandingOverlapBuffer, rotation, collisionMask, QueryTriggerInteraction.Ignore);
            }
            else
            {
                return false;
            }

            for (int i = 0; i < overlapCount; i++)
            {
                Collider hit = LandingOverlapBuffer[i];
                if (hit == null)
                {
                    continue;
                }

                if (IsSelfCollider(hit, colliderCache.OwnerColliders))
                {
                    continue;
                }

                if (Physics.GetIgnoreLayerCollision(colliderCache.OwnerLayer, hit.gameObject.layer))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private static LayerMask ResolveDashCollisionMask(AbilityContext context, AbilityDataSO data)
        {
            LayerMask resolved = context != null ? context.ResolveTargetLayers(data) : default;
            if (resolved.value == 0)
            {
                LayerMask all = default;
                all.value = ~0;
                return all;
            }

            return resolved;
        }

        private static bool TryBuildColliderCache(Rigidbody ownerRigidbody, out DashColliderCache cache)
        {
            cache = DashColliderCache.Empty;

            if (ownerRigidbody == null)
            {
                return false;
            }

            Collider[] ownerColliders = ownerRigidbody.GetComponentsInChildren<Collider>();
            Collider primaryCollider = null;

            int colliderCount = ownerColliders.Length;
            for (int i = 0; i < colliderCount; i++)
            {
                Collider candidate = ownerColliders[i];
                if (candidate == null || candidate.isTrigger)
                {
                    continue;
                }

                primaryCollider = candidate;
                break;
            }

            if (primaryCollider == null)
            {
                return false;
            }

            Vector3 ownerScale = ownerRigidbody.transform.lossyScale;
            Vector3 absoluteScale = Abs(ownerScale);
            int ownerLayer = ownerRigidbody.gameObject.layer;

            cache = new DashColliderCache(primaryCollider, ownerColliders, ownerLayer, ownerScale, absoluteScale);
            return true;
        }

        private static bool IsSelfCollider(Collider candidate, IReadOnlyList<Collider> ownerColliders)
        {
            if (candidate == null || ownerColliders == null)
            {
                return false;
            }

            int count = ownerColliders.Count;
            for (int i = 0; i < count; i++)
            {
                if (ReferenceEquals(candidate, ownerColliders[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static Vector3 Abs(Vector3 value)
        {
            return new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
        }

        private static Vector3 GetWorldAxis(Quaternion rotation, int direction)
        {
            if (direction == 0)
            {
                return rotation * Vector3.right;
            }

            if (direction == 2)
            {
                return rotation * Vector3.forward;
            }

            return rotation * Vector3.up;
        }

        private static float GetAxisScale(Vector3 absoluteScale, int direction)
        {
            if (direction == 0)
            {
                return absoluteScale.x;
            }

            if (direction == 2)
            {
                return absoluteScale.z;
            }

            return absoluteScale.y;
        }

        private static float GetPerpendicularMaxScale(Vector3 absoluteScale, int direction)
        {
            if (direction == 0)
            {
                return Mathf.Max(absoluteScale.y, absoluteScale.z);
            }

            if (direction == 2)
            {
                return Mathf.Max(absoluteScale.x, absoluteScale.y);
            }

            return Mathf.Max(absoluteScale.x, absoluteScale.z);
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
                    module.AllowPhaseThroughObstaclesIfLandingIsValid,
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
            public DashParameters(float distance, float durationSeconds, AnimationCurve speedCurve, bool allowPhaseThroughObstaclesIfLandingIsValid, bool enableDebugTelemetry)
            {
                Distance = distance;
                DurationSeconds = durationSeconds;
                SpeedCurve = speedCurve;
                AllowPhaseThroughObstaclesIfLandingIsValid = allowPhaseThroughObstaclesIfLandingIsValid;
                EnableDebugTelemetry = enableDebugTelemetry;
            }

            public float Distance { get; }
            public float DurationSeconds { get; }
            public AnimationCurve SpeedCurve { get; }
            public bool AllowPhaseThroughObstaclesIfLandingIsValid { get; }
            public bool EnableDebugTelemetry { get; }
        }

        private readonly struct DashColliderCache
        {
            public static DashColliderCache Empty => new DashColliderCache(null, Array.Empty<Collider>(), -1, Vector3.one, Vector3.one);

            public DashColliderCache(
                Collider primaryCollider,
                Collider[] ownerColliders,
                int ownerLayer,
                Vector3 ownerScale,
                Vector3 absoluteScale)
            {
                PrimaryCollider = primaryCollider;
                OwnerColliders = ownerColliders;
                OwnerLayer = ownerLayer;
                OwnerScale = ownerScale;
                AbsoluteScale = absoluteScale;
            }

            public bool HasPrimaryCollider => PrimaryCollider != null;
            public Collider PrimaryCollider { get; }
            public Collider[] OwnerColliders { get; }
            public int OwnerLayer { get; }
            public Vector3 OwnerScale { get; }
            public Vector3 AbsoluteScale { get; }
        }
    }
}
