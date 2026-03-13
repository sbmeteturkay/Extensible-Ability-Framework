using UnityEngine;
using UnityEngine.Serialization;

namespace CaseStudy.Feature.AbilitySystem.Data
{
    [CreateAssetMenu(fileName = "SO_DashAbility", menuName = "Ability/Dash Data")]
    public sealed class DashAbilityDataSO : AbilityDataSO
    {
        [Header("Dash")]
        [SerializeField, Min(0.1f)] private float _dashDistance = 4f;
        [SerializeField, Min(0.05f)] private float _dashDurationSeconds = 0.2f;
        [SerializeField, HideInInspector, FormerlySerializedAs("_disableRegularMovement")] private bool _legacyDisableRegularMovement;
        [SerializeField] private AnimationCurve _speedCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

        public float DashDistance => _dashDistance;

        public float DashDurationSeconds => _dashDurationSeconds;

        public override AbilityMovementPolicy MovementPolicy =>
            base.MovementPolicy == AbilityMovementPolicy.None && _legacyDisableRegularMovement
                ? AbilityMovementPolicy.LockLocomotionDuringExecution
                : base.MovementPolicy;

        public AnimationCurve SpeedCurve => _speedCurve;
    }
}
