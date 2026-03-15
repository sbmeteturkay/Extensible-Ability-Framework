using UnityEngine;

namespace CaseStudy.Feature.AbilitySystem.Data
{
    [CreateAssetMenu(fileName = "SO_HitVisualProfile", menuName = "Ability/Hit Visual Profile")]
    public sealed class HitVisualProfileSO : ScriptableObject
    {
        [Header("Flash")]
        [SerializeField] private bool _enableColorFlash = true;
        [SerializeField] private Color _flashColor = Color.white;
        [SerializeField, Min(0.01f)] private float _flashDuration = 0.12f;

        [Header("Scale")]
        [SerializeField] private bool _enableScalePunch = true;
        [SerializeField, Min(1f)] private float _scaleMultiplier = 1.12f;
        [SerializeField, Min(0.01f)] private float _scaleDuration = 0.15f;

        [Header("Bounce")]
        [SerializeField] private bool _enableBounce = true;
        [SerializeField, Min(0f)] private float _bounceHeight = 0.25f;
        [SerializeField, Min(0.01f)] private float _bounceDuration = 0.18f;

        [Header("Timing")]
        [SerializeField, Min(0f)] private float _hitVisualDelaySeconds;

        [Header("Optional")]
        [SerializeField] private GameObject _hitVfxPrefab;
        [SerializeField, Min(0f)] private float _hitVfxDelaySeconds;
        [SerializeField, Min(0f)] private float _hitVfxAutoReturnSeconds = 2f;

        public bool EnableColorFlash => _enableColorFlash;

        public Color FlashColor => _flashColor;

        public float FlashDuration => _flashDuration;

        public bool EnableScalePunch => _enableScalePunch;

        public float ScaleMultiplier => _scaleMultiplier;

        public float ScaleDuration => _scaleDuration;

        public bool EnableBounce => _enableBounce;

        public float BounceHeight => _bounceHeight;

        public float BounceDuration => _bounceDuration;

        public float HitVisualDelaySeconds => _hitVisualDelaySeconds;

        public GameObject HitVfxPrefab => _hitVfxPrefab;

        public float HitVfxDelaySeconds => _hitVfxDelaySeconds;

        public float HitVfxAutoReturnSeconds => _hitVfxAutoReturnSeconds;
    }
}
