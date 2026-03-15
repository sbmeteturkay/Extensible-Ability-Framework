using UnityEngine;

namespace CaseStudy.Feature.AbilitySystem.Data
{
    [CreateAssetMenu(fileName = "SO_Module_HitFeedback", menuName = "Ability/Modules/Hit Feedback")]
    public sealed class AbilityHitFeedbackModuleSO : AbilityOptionalModuleSO
    {
        [Header("Target Hit")]
        [SerializeField] private HitVisualProfileSO _targetHitVisualProfile;

        [Header("Projectile Impact")]
        [SerializeField] private GameObject _impactVfxPrefab;
        [SerializeField, Min(0f)] private float _impactVfxDelaySeconds;
        [SerializeField, Min(0f)] private float _impactVfxAutoReturnSeconds = 2f;
        [SerializeField] private AudioClip _impactSfx;
        [SerializeField, Range(0f, 1f)] private float _impactSfxVolume = 1f;

        public HitVisualProfileSO TargetHitVisualProfile => _targetHitVisualProfile;
        public GameObject ImpactVfxPrefab => _impactVfxPrefab;
        public float ImpactVfxDelaySeconds => _impactVfxDelaySeconds;
        public float ImpactVfxAutoReturnSeconds => _impactVfxAutoReturnSeconds;
        public AudioClip ImpactSfx => _impactSfx;
        public float ImpactSfxVolume => _impactSfxVolume;
    }
}
