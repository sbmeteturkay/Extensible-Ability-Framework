using System;
using UnityEngine;

namespace CaseStudy.Feature.AbilitySystem.Abilities
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ProjectileRuntime : MonoBehaviour
    {
        private Rigidbody _rigidbody;
        private bool _isActive;
        private float _lifeTimeSeconds;
        private float _elapsedSeconds;
        private LayerMask _hitLayers;
        private GameObject _impactVfxPrefab;
        private Action<ProjectileRuntime> _releaseAction;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.useGravity = false;
        }

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

            _elapsedSeconds += Time.fixedDeltaTime;
            if (_elapsedSeconds >= _lifeTimeSeconds)
            {
                ReturnToPool();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!_isActive)
            {
                return;
            }

            if (!IsInLayerMask(collision.gameObject.layer, _hitLayers))
            {
                return;
            }

            SpawnImpactVfx();
            ReturnToPool();
        }

        public void Launch(
            Vector3 position,
            Vector3 direction,
            float speed,
            float lifeTimeSeconds,
            LayerMask hitLayers,
            GameObject impactVfxPrefab,
            Action<ProjectileRuntime> releaseAction)
        {
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction));

            _lifeTimeSeconds = Mathf.Max(0.05f, lifeTimeSeconds);
            _hitLayers = hitLayers;
            _impactVfxPrefab = impactVfxPrefab;
            _releaseAction = releaseAction;
            _isActive = true;
            _elapsedSeconds = 0f;

            _rigidbody.linearVelocity = direction.normalized * Mathf.Max(0f, speed);
            _rigidbody.angularVelocity = Vector3.zero;
        }

        private void ReturnToPool()
        {
            if (!_isActive)
            {
                return;
            }

            _isActive = false;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;

            _releaseAction?.Invoke(this);
        }

        private void SpawnImpactVfx()
        {
            if (_impactVfxPrefab == null)
            {
                return;
            }

            Instantiate(_impactVfxPrefab, transform.position, Quaternion.identity);
        }

        private static bool IsInLayerMask(int layer, LayerMask layerMask)
        {
            return ((1 << layer) & layerMask.value) != 0;
        }
    }
}
