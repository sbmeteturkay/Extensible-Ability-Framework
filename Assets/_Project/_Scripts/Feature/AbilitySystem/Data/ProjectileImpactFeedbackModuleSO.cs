using UnityEngine;

namespace CaseStudy.Feature.AbilitySystem.Data
{
    [CreateAssetMenu(fileName = "SO_Module_ProjectileImpactFeedback", menuName = "Ability/Modules/Projectile Impact Feedback")]
    public sealed class ProjectileImpactFeedbackModuleSO : AbilityOptionalModuleSO
    {
        [SerializeField] private GameObject _impactVfxPrefab;
        [SerializeField, Min(0f)] private float _impactVfxDelaySeconds;
        [SerializeField, Min(0f)] private float _impactVfxAutoReturnSeconds = 2f;
        [SerializeField] private AudioClip _impactSfx;
        [SerializeField, Range(0f, 1f)] private float _impactSfxVolume = 1f;

        public GameObject ImpactVfxPrefab => _impactVfxPrefab;
        public float ImpactVfxDelaySeconds => _impactVfxDelaySeconds;
        public float ImpactVfxAutoReturnSeconds => _impactVfxAutoReturnSeconds;
        public AudioClip ImpactSfx => _impactSfx;
        public float ImpactSfxVolume => _impactSfxVolume;

        public override bool IsCompatibleWith(AbilityExecutorSO executor)
        {
            return executor is ProjectileExecutorSO;
        }
    }
}
