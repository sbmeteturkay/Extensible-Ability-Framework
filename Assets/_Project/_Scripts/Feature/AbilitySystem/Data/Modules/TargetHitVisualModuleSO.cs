using UnityEngine;

namespace CaseStudy.Feature.AbilitySystem.Data
{
    [CreateAssetMenu(fileName = "SO_Module_TargetHitVisual", menuName = "Ability/Modules/Target Hit Visual")]
    public sealed class TargetHitVisualModuleSO : AbilityOptionalModuleSO
    {
        [SerializeField] private HitVisualProfileSO _targetHitVisualProfile;

        public HitVisualProfileSO TargetHitVisualProfile => _targetHitVisualProfile;

        public override bool IsCompatibleWith(AbilityExecutorSO executor)
        {
            return executor is ITargetHitVisualConsumer;
        }
    }
}

