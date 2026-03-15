using System;
using CaseStudy.Feature.AbilitySystem.Contracts;
using CaseStudy.Feature.AbilitySystem.Data;
using CaseStudy.Shared.Vfx.Interfaces;
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
        private HitVisualProfileSO _targetHitVisualProfile;
        private GameObject _impactVfxPrefab;
        private float _impactVfxDelaySeconds;
        private float _impactVfxAutoReturnSeconds;
        private AudioClip _impactSfx;
        private float _impactSfxVolume;
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
                    TryApplyHitVisual(hit.collider);
                    SpawnTargetHitVfx(hit.point);
                    SpawnImpactVfx(hit.point);
                    SpawnImpactSfx(hit.point);
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
            HitVisualProfileSO targetHitVisualProfile,
            GameObject impactVfxPrefab,
            float impactVfxDelaySeconds,
            float impactVfxAutoReturnSeconds,
            AudioClip impactSfx,
            float impactSfxVolume,
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
            _targetHitVisualProfile = targetHitVisualProfile;
            _impactVfxPrefab = impactVfxPrefab;
            _impactVfxDelaySeconds = Mathf.Max(0f, impactVfxDelaySeconds);
            _impactVfxAutoReturnSeconds = Mathf.Max(0f, impactVfxAutoReturnSeconds);
            _impactSfx = impactSfx;
            _impactSfxVolume = Mathf.Clamp01(impactSfxVolume);
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

        private void TryApplyHitVisual(Collider hitCollider)
        {
            if (hitCollider == null || _targetHitVisualProfile == null)
            {
                return;
            }

            Component receiverComponent = hitCollider.GetComponent(typeof(IAbilityHitVisualReceiver)) as Component;
            if (receiverComponent == null)
            {
                receiverComponent = hitCollider.GetComponentInParent(typeof(IAbilityHitVisualReceiver)) as Component;
            }

            if (receiverComponent is IAbilityHitVisualReceiver visualReceiver)
            {
                visualReceiver.ApplyHitVisual(_targetHitVisualProfile);
            }
        }

        private void SpawnTargetHitVfx(Vector3 impactPosition)
        {
            if (_targetHitVisualProfile == null || _targetHitVisualProfile.HitVfxPrefab == null)
            {
                return;
            }

            if (_pooledVfxService == null)
            {
                Debug.LogWarning("ProjectileRuntime: IPooledVfxService is missing. Skipping target hit VFX spawn.");
                return;
            }

            _pooledVfxService.Spawn(
                _targetHitVisualProfile.HitVfxPrefab,
                impactPosition,
                Quaternion.identity,
                _targetHitVisualProfile.HitVfxDelaySeconds,
                _targetHitVisualProfile.HitVfxAutoReturnSeconds);
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

            Debug.LogWarning("ProjectileRuntime: IPooledVfxService is missing. Skipping projectile impact VFX spawn.");
        }

        private void SpawnImpactSfx(Vector3 impactPosition)
        {
            if (_impactSfx == null)
            {
                return;
            }

            AudioSource.PlayClipAtPoint(_impactSfx, impactPosition, _impactSfxVolume);
        }
    }
}
