using System.Threading;
using CaseStudy.Feature.AbilitySystem.Contracts;
using CaseStudy.Feature.AbilitySystem.Data;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CaseStudy.Feature.AbilitySystem.Runtime
{
    /// <summary>
    /// Dummy receiver for case validation. Applies temporary hit visuals driven by ability profiles.
    /// If a new hit arrives while an effect is active, current visuals are finalized and the new one starts.
    /// </summary>
    public sealed class DummyAbilityTarget : MonoBehaviour, IAoeEffectReceiver, IAbilityHitVisualReceiver
    {
        [Header("References")]
        [SerializeField] private Renderer _targetRenderer;
        [SerializeField] private string _baseColorPropertyName = "_BaseColor";

        private MaterialPropertyBlock _propertyBlock;
        private Vector3 _baseScale;
        private Vector3 _baseLocalPosition;
        private Color _baseColor = Color.white;
        private bool _hasBaseColor;
        private int _baseColorPropertyId;
        private int _fallbackColorPropertyId;
        private uint _visualSequence;

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            _baseScale = transform.localScale;
            _baseLocalPosition = transform.localPosition;

            if (_targetRenderer != null)
            {
                _baseColorPropertyId = Shader.PropertyToID(_baseColorPropertyName);
                _fallbackColorPropertyId = Shader.PropertyToID("_Color");

                Material sharedMaterial = _targetRenderer.sharedMaterial;
                if (sharedMaterial != null)
                {
                    if (sharedMaterial.HasProperty(_baseColorPropertyId))
                    {
                        _baseColor = sharedMaterial.GetColor(_baseColorPropertyId);
                        _hasBaseColor = true;
                    }
                    else if (sharedMaterial.HasProperty(_fallbackColorPropertyId))
                    {
                        _baseColor = sharedMaterial.GetColor(_fallbackColorPropertyId);
                        _hasBaseColor = true;
                    }
                }
            }
        }

        private void OnDisable()
        {
            FinalizeCurrentVisual();
        }

        public void ApplyAoeEffect(float effectDurationSeconds)
        {
            // Dummy target intentionally has no gameplay effect. Visuals are profile-driven.
        }

        public void ApplyHitVisual(HitVisualProfileSO profile)
        {
            if (profile == null)
            {
                return;
            }

            FinalizeCurrentVisual();
            PlayVisualAsync(profile, _visualSequence).Forget();
        }

        private async UniTaskVoid PlayVisualAsync(HitVisualProfileSO profile, uint sequence)
        {
            if (profile.HitVisualDelaySeconds > 0f)
            {
                int delayMilliseconds = Mathf.CeilToInt(profile.HitVisualDelaySeconds * 1000f);
                if (delayMilliseconds > 0)
                {
                    await UniTask.Delay(delayMilliseconds, DelayType.DeltaTime, PlayerLoopTiming.Update, CancellationToken.None);
                }
            }

            if (sequence != _visualSequence)
            {
                return;
            }

            float totalDuration = GetTotalDuration(profile);
            if (totalDuration <= 0f)
            {
                return;
            }

            float elapsed = 0f;
            while (elapsed < totalDuration)
            {
                if (sequence != _visualSequence)
                {
                    return;
                }

                elapsed += Time.deltaTime;
                float time = Mathf.Min(elapsed, totalDuration);

                ApplyColorFlash(profile, time);
                ApplyScale(profile, time);
                ApplyBounce(profile, time);

                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            if (sequence == _visualSequence)
            {
                ResetVisualState();
            }
        }

        private void ApplyColorFlash(HitVisualProfileSO profile, float time)
        {
            if (!profile.EnableColorFlash || !_hasBaseColor || _targetRenderer == null)
            {
                return;
            }

            float t = profile.FlashDuration > 0f ? Mathf.Clamp01(time / profile.FlashDuration) : 1f;
            Color blended = Color.Lerp(profile.FlashColor, _baseColor, t);

            _targetRenderer.GetPropertyBlock(_propertyBlock);
            if (_targetRenderer.sharedMaterial != null && _targetRenderer.sharedMaterial.HasProperty(_baseColorPropertyId))
            {
                _propertyBlock.SetColor(_baseColorPropertyId, blended);
            }
            else
            {
                _propertyBlock.SetColor(_fallbackColorPropertyId, blended);
            }
            _targetRenderer.SetPropertyBlock(_propertyBlock);
        }

        private void ApplyScale(HitVisualProfileSO profile, float time)
        {
            if (!profile.EnableScalePunch)
            {
                return;
            }

            float t = profile.ScaleDuration > 0f ? Mathf.Clamp01(time / profile.ScaleDuration) : 1f;
            float pulse = Mathf.Sin(t * Mathf.PI);
            float scaleFactor = Mathf.Lerp(1f, profile.ScaleMultiplier, pulse);

            transform.localScale = _baseScale * scaleFactor;
        }

        private void ApplyBounce(HitVisualProfileSO profile, float time)
        {
            if (!profile.EnableBounce)
            {
                return;
            }

            float t = profile.BounceDuration > 0f ? Mathf.Clamp01(time / profile.BounceDuration) : 1f;
            float arc = Mathf.Sin(t * Mathf.PI);
            float yOffset = arc * profile.BounceHeight;

            Vector3 nextPosition = _baseLocalPosition;
            nextPosition.y += yOffset;
            transform.localPosition = nextPosition;
        }

        private float GetTotalDuration(HitVisualProfileSO profile)
        {
            float total = 0f;

            if (profile.EnableColorFlash)
            {
                total = Mathf.Max(total, profile.FlashDuration);
            }

            if (profile.EnableScalePunch)
            {
                total = Mathf.Max(total, profile.ScaleDuration);
            }

            if (profile.EnableBounce)
            {
                total = Mathf.Max(total, profile.BounceDuration);
            }

            return total;
        }

        private void FinalizeCurrentVisual()
        {
            _visualSequence++;
            ResetVisualState();
        }

        private void ResetVisualState()
        {
            transform.localScale = _baseScale;
            transform.localPosition = _baseLocalPosition;

            if (!_hasBaseColor || _targetRenderer == null)
            {
                return;
            }

            _targetRenderer.GetPropertyBlock(_propertyBlock);
            if (_targetRenderer.sharedMaterial != null && _targetRenderer.sharedMaterial.HasProperty(_baseColorPropertyId))
            {
                _propertyBlock.SetColor(_baseColorPropertyId, _baseColor);
            }
            else
            {
                _propertyBlock.SetColor(_fallbackColorPropertyId, _baseColor);
            }
            _targetRenderer.SetPropertyBlock(_propertyBlock);
        }
    }
}

