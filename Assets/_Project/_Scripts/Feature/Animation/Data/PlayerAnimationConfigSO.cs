using UnityEngine;

namespace CaseStudy.Feature.Animation.Data
{
    [CreateAssetMenu(fileName = "SO_PlayerAnimationConfig", menuName = "Animation/Player Animation Config")]
    public sealed class PlayerAnimationConfigSO : ScriptableObject
    {
        [Header("Animator Parameters")]
        [SerializeField] private string _moveXParam = "MoveX";
        [SerializeField] private string _moveYParam = "MoveY";
        [SerializeField] private string _speedParam = "Speed";
        [SerializeField] private string _isMovingParam = "IsMoving";

        [Header("Motion")]
        [SerializeField, Min(0f)] private float _referenceMoveSpeed = 6f;
        [SerializeField, Min(0f)] private float _movingThreshold = 0.08f;
        [SerializeField, Min(0f)] private float _movingHoldSeconds = 0.1f;

        [Header("Damping")]
        [SerializeField] private bool _useDamping = true;
        [SerializeField, Min(0f)] private float _moveXDampTime = 0.08f;
        [SerializeField, Min(0f)] private float _moveYDampTime = 0.08f;
        [SerializeField, Min(0f)] private float _speedDampTime = 0.06f;

        [Header("Ability Hooks (Optional)")]
        [SerializeField] private string _abilityUsedTriggerParam = "AbilityUsed";
        [SerializeField] private string _abilityIndexIntParam = "AbilityIndex";

        public string MoveXParam => _moveXParam;

        public string MoveYParam => _moveYParam;

        public string SpeedParam => _speedParam;

        public string IsMovingParam => _isMovingParam;

        public float ReferenceMoveSpeed => _referenceMoveSpeed;

        public float MovingThreshold => _movingThreshold;

        public float MovingHoldSeconds => _movingHoldSeconds;

        public bool UseDamping => _useDamping;

        public float MoveXDampTime => _moveXDampTime;

        public float MoveYDampTime => _moveYDampTime;

        public float SpeedDampTime => _speedDampTime;

        public string AbilityUsedTriggerParam => _abilityUsedTriggerParam;

        public string AbilityIndexIntParam => _abilityIndexIntParam;
    }
}
