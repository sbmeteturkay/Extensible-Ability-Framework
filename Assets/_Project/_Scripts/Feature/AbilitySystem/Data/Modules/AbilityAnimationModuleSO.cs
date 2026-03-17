using UnityEngine;

namespace CaseStudy.Feature.AbilitySystem.Data
{
    [CreateAssetMenu(fileName = "SO_Module_AbilityAnimation", menuName = "Ability/Modules/Ability Animation")]
    public sealed class AbilityAnimationModuleSO : AbilityOptionalModuleSO
    {
        [SerializeField] private AnimationClip _animationClip;
        [SerializeField] private bool _useCustomPlaybackSpeed;
        [SerializeField, Min(0.01f)] private float _playbackSpeed = 1f;

        public AnimationClip AnimationClip => _animationClip;
        public bool UseCustomPlaybackSpeed => _useCustomPlaybackSpeed;
        public float PlaybackSpeed => _playbackSpeed;

        public override AbilityModuleExecutionTime ExecutionTime => AbilityModuleExecutionTime.PresentationSetup;
    }
}

