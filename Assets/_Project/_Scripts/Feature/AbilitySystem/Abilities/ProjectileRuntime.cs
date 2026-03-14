using System;
using System.Threading;
using CaseStudy.Shared.Vfx.Interfaces;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CaseStudy.Feature.AbilitySystem.Abilities
{
    /// <summary>
    /// Physics-independent projectile behaviour: manual movement, query-based hit checks, lifetime, and pool return.
    /// </summary>
    public sealed class ProjectileRuntime : MonoBehaviour
    {
        private const float MIN_STEP_DISTANCE = 0.0001f;

        private bool _isActive;
        private float _lifeTimeSeconds;
        private float _elapsedSeconds;
        private float _speed;
        private float _hitRadius;
        private Vector3 _direction;
        private LayerMask _hitLayers;
        private Transform _ownerTransform;
        private GameObject _impactVfxPrefab;
        private float _impactVfxDelaySeconds;
        private float _impactVfxAutoReturnSeconds;
        private IPooledVfxService _pooledVfxService;
        private Action<ProjectileRuntime> _releaseAction;

        private void OnEnable()
        {
            _elapsedSeconds = 0f;
        }

        private void FixedUpdate()
        {
            if (!_isActive)
            {
                return;
            }

            float stepDistance = _speed * Time.fixedDeltaTime;
            if (stepDistance > MIN_STEP_DISTANCE)
            {
                Vector3 currentPosition = transform.position;
                Vector3 nextPosition = currentPosition + _direction * stepDistance;

                if (TryHit(currentPosition, stepDistance, out RaycastHit hit))
                {
                    transform.position = hit.point;
                    SpawnImpactVfx(hit.point);
                    ReturnToPool();
                    return;
                }

                transform.position = nextPosition;
            }

            _elapsedSeconds += Time.fixedDeltaTime;
            if (_elapsedSeconds >= _lifeTimeSeconds)
            {
                ReturnToPool();
            }
        }

        public void Launch(
            Vector3 position,
            Vector3 direction,
            float speed,
            float lifeTimeSeconds,
            float hitRadius,
            LayerMask hitLayers,
            Transform ownerTransform,
            GameObject impactVfxPrefab,
            float impactVfxDelaySeconds,
            float impactVfxAutoReturnSeconds,
            IPooledVfxService pooledVfxService,
            Action<ProjectileRuntime> releaseAction)
        {
            Vector3 launchDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;

            transform.SetPositionAndRotation(position, Quaternion.LookRotation(launchDirection));

            _direction = launchDirection;
            _speed = Mathf.Max(0f, speed);
            _lifeTimeSeconds = Mathf.Max(0.05f, lifeTimeSeconds);
            _hitRadius = Mathf.Max(0f, hitRadius);
            _hitLayers = hitLayers;
            _ownerTransform = ownerTransform;
            _impactVfxPrefab = impactVfxPrefab;
            _impactVfxDelaySeconds = Mathf.Max(0f, impactVfxDelaySeconds);
            _impactVfxAutoReturnSeconds = Mathf.Max(0f, impactVfxAutoReturnSeconds);
            _pooledVfxService = pooledVfxService;
            _releaseAction = releaseAction;
            _isActive = true;
            _elapsedSeconds = 0f;
        }

        private bool TryHit(Vector3 origin, float distance, out RaycastHit hit)
        {
            bool hasHit = _hitRadius > 0f
                ? Physics.SphereCast(origin, _hitRadius, _direction, out hit, distance, _hitLayers, QueryTriggerInteraction.Ignore)
                : Physics.Raycast(origin, _direction, out hit, distance, _hitLayers, QueryTriggerInteraction.Ignore);

            if (!hasHit)
            {
                return false;
            }

            if (_ownerTransform != null && hit.collider != null && hit.collider.transform.IsChildOf(_ownerTransform))
            {
                return false;
            }

            return true;
        }

        private void ReturnToPool()
        {
            if (!_isActive)
            {
                return;
            }

            _isActive = false;
            _releaseAction?.Invoke(this);
        }

        private void SpawnImpactVfx(Vector3 impactPosition)
        {
            if (_impactVfxPrefab == null)
            {
                return;
            }

            if (_pooledVfxService != null)
            {
                _pooledVfxService.Spawn(
                    _impactVfxPrefab,
                    impactPosition,
                    Quaternion.identity,
                    _impactVfxDelaySeconds,
                    _impactVfxAutoReturnSeconds);
                return;
            }

            if (_impactVfxDelaySeconds <= 0f)
            {
                Instantiate(_impactVfxPrefab, impactPosition, Quaternion.identity);
                return;
            }

            SpawnImpactVfxDelayedAsync(_impactVfxPrefab, impactPosition, _impactVfxDelaySeconds).Forget();
        }

        private static async UniTaskVoid SpawnImpactVfxDelayedAsync(GameObject impactVfxPrefab, Vector3 position, float delaySeconds)
        {
            int delayMilliseconds = Mathf.CeilToInt(delaySeconds * 1000f);
            if (delayMilliseconds > 0)
            {
                await UniTask.Delay(delayMilliseconds, DelayType.DeltaTime, PlayerLoopTiming.Update, CancellationToken.None);
            }

            Instantiate(impactVfxPrefab, position, Quaternion.identity);
        }
    }
}
