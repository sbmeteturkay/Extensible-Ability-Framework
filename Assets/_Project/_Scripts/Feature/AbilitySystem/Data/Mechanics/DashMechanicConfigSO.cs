using UnityEngine;

namespace CaseStudy.Feature.AbilitySystem.Data
{
    [CreateAssetMenu(fileName = "SO_Mechanic_Dash", menuName = "Ability/Mechanics/Dash")]
    public sealed class DashMechanicConfigSO : AbilityMechanicConfigSO
    {
        [SerializeField, Min(0.1f)] private float _dashDistance = 4f;
        [SerializeField, Min(0.05f)] private float _dashDurationSeconds = 0.2f;
        [SerializeField] private AnimationCurve _speedCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
        [SerializeField] private bool _allowPhaseThroughObstaclesIfLandingIsValid;

        [Header("Debug")]
        [SerializeField] private bool _enableDebugTelemetry;

        public float DashDistance => _dashDistance;
        public float DashDurationSeconds => _dashDurationSeconds;
        public AnimationCurve SpeedCurve => _speedCurve;
        public bool AllowPhaseThroughObstaclesIfLandingIsValid => _allowPhaseThroughObstaclesIfLandingIsValid;
        public bool EnableDebugTelemetry => _enableDebugTelemetry;
    }
}
