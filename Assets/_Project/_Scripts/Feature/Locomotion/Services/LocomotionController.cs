using CaseStudy.Feature.Locomotion.Contracts;
using CaseStudy.Feature.Locomotion.Domain;
using CaseStudy.Shared.Locomotion.Interfaces;
using UnityEngine;
using VContainer.Unity;

namespace CaseStudy.Feature.Locomotion.Services
{
    /// <summary>
    /// Applies movement/rotation in FixedUpdate with cached input and config.
    /// Exposes a small lock API so abilities can pause regular movement when needed.
    /// </summary>
    public sealed class LocomotionController : ILocomotionController, IFixedTickable, ILocomotionLockService
    {
        private const float MIN_DIRECTION_SQR_MAGNITUDE = 0.0001f;

        private LocomotionContext _context;
        private int _externalLockCount;

        public bool IsLocked => _externalLockCount > 0;

        public void Configure(LocomotionContext context)
        {
            _context = context;
        }

        public void PushLock()
        {
            _externalLockCount++;
        }

        public void PopLock()
        {
            if (_externalLockCount <= 0)
            {
                _externalLockCount = 0;
                return;
            }

            _externalLockCount--;
        }

        public void FixedTick()
        {
            if (_context == null || IsLocked)
            {
                return;
            }

            Vector2 moveInput = _context.InputReader.MoveInput;
            float deadZone = _context.LocomotionData.InputDeadZone;
            float deadZoneSqr = deadZone * deadZone;

            if (moveInput.sqrMagnitude <= deadZoneSqr)
            {
                return;
            }

            Vector3 moveDirection = new Vector3(moveInput.x, 0f, moveInput.y);
            float sqrMagnitude = moveDirection.sqrMagnitude;
            if (sqrMagnitude <= MIN_DIRECTION_SQR_MAGNITUDE)
            {
                return;
            }

            float inverseMagnitude = 1f / Mathf.Sqrt(sqrMagnitude);
            moveDirection *= inverseMagnitude;

            float inputStrength = Mathf.Clamp01(moveInput.magnitude);
            float moveDistance = _context.LocomotionData.MoveSpeed * inputStrength * Time.fixedDeltaTime;
            Vector3 nextPosition = _context.OwnerRigidbody.position + (moveDirection * moveDistance);

            _context.OwnerRigidbody.MovePosition(nextPosition);
            RotateTowards(moveDirection);
        }

        private void RotateTowards(Vector3 moveDirection)
        {
            float rotationSpeed = _context.LocomotionData.RotationSpeedDegreesPerSecond;
            if (rotationSpeed <= 0f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            Quaternion nextRotation = Quaternion.RotateTowards(
                _context.OwnerTransform.rotation,
                targetRotation,
                rotationSpeed * Time.fixedDeltaTime);

            _context.OwnerRigidbody.MoveRotation(nextRotation);
        }
    }
}
