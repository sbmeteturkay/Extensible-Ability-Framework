using CaseStudy.Feature.AbilitySystem.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CaseStudy.Feature.AbilitySystem.UI
{
    /// <summary>
    /// Single HUD slot widget that displays icon/cooldown and forwards click callbacks.
    /// </summary>
    public sealed class AbilityHudSlotWidget : MonoBehaviour
    {
        [SerializeField] private AbilitySlot _slot;
        [SerializeField] private Button _triggerButton;
        [SerializeField] private Image _iconImage;
        [SerializeField] private Image _cooldownFill;
        [SerializeField] private TMP_Text _cooldownLabel;

        private System.Action<AbilitySlot> _triggerCallback;

        public AbilitySlot Slot => _slot;

        private void Awake()
        {
            if (_triggerButton != null)
            {
                _triggerButton.onClick.RemoveAllListeners();
                _triggerButton.onClick.AddListener(OnClicked);
            }
        }

        public void SetTriggerCallback(System.Action<AbilitySlot> triggerCallback)
        {
            _triggerCallback = triggerCallback;
        }

        public void BindIcon(Sprite icon)
        {
            if (_iconImage == null)
            {
                return;
            }

            _iconImage.sprite = icon;
            _iconImage.enabled = icon != null;
        }

        public void SetCooldown(float remainingSeconds, float normalizedRemaining)
        {
            if (_cooldownFill != null)
            {
                _cooldownFill.fillAmount = Mathf.Clamp01(normalizedRemaining);
            }

            if (_cooldownLabel != null)
            {
                _cooldownLabel.text = remainingSeconds > 0f ? remainingSeconds.ToString("0.0") : string.Empty;
            }
        }

        public void ClearCooldown()
        {
            if (_cooldownFill != null)
            {
                _cooldownFill.fillAmount = 0f;
            }

            if (_cooldownLabel != null)
            {
                _cooldownLabel.text = string.Empty;
            }
        }

        private void OnClicked()
        {
            _triggerCallback?.Invoke(_slot);
        }
    }
}