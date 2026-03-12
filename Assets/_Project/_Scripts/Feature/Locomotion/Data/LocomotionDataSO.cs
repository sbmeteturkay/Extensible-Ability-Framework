using UnityEngine;

namespace CaseStudy.Feature.Locomotion.Data
{
    [CreateAssetMenu(fileName = "SO_LocomotionData", menuName = "Locomotion/Locomotion Data")]
    public sealed class LocomotionDataSO : ScriptableObject
    {
        [Header("Movement")]
        [SerializeField, Min(0f)] private float _moveSpeed = 6f;
        [SerializeField, Min(0f)] private float _rotationSpeedDegreesPerSecond = 720f;
        [SerializeField, Range(0f, 0.5f)] private float _inputDeadZone = 0.1f;

        public float MoveSpeed => _moveSpeed;

        public float RotationSpeedDegreesPerSecond => _rotationSpeedDegreesPerSecond;

        public float InputDeadZone => _inputDeadZone;
    }
}
