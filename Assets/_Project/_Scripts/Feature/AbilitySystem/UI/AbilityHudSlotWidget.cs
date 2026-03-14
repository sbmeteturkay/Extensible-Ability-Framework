using CaseStudy.Feature.AbilitySystem.Data;
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
        [SerializeField] private SlotDefinitionSO _slotDefinition;
        [SerializeField] private Button _triggerButton;
        [SerializeField] private Image _iconImage;
        [SerializeField] private Image _cooldownFill;
        [SerializeField] private TMP_Text _cooldownLabel;

        private System.Action<string> _triggerCallback;

        public string SlotKey => _slotDefinition != null ? AbilitySlotKeyUtility.Normalize(_slotDefinition.SlotKey) : string.Empty;

        private void Awake()
        {
            if (_triggerButton != null)
            {
                _triggerButton.onClick.RemoveAllListeners();
                _triggerButton.onClick.AddListener(OnClicked);
            }
        }

        public void SetTriggerCallback(System.Action<string> triggerCallback)
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
            string slotKey = SlotKey;
            if (string.IsNullOrWhiteSpace(slotKey))
            {
                return;
            }

            _triggerCallback?.Invoke(slotKey);
        }
    }
}

