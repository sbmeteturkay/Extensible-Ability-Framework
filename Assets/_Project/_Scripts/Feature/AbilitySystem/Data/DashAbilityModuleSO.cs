using UnityEngine;

namespace CaseStudy.Feature.AbilitySystem.Data
{
    [CreateAssetMenu(fileName = "SO_Module_Dash", menuName = "Ability/Modules/Dash Module")]
    public sealed class DashAbilityModuleSO : AbilityModuleSO
    {
        [SerializeField, Min(0.1f)] private float _dashDistance = 4f;
        [SerializeField, Min(0.05f)] private float _dashDurationSeconds = 0.2f;
        [SerializeField] private AnimationCurve _speedCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

        public float DashDistance => _dashDistance;

        public float DashDurationSeconds => _dashDurationSeconds;

        public AnimationCurve SpeedCurve => _speedCurve;
    }
}
