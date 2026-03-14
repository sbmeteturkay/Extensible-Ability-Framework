using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CaseStudy.Feature.AbilitySystem.UI
{
    /// <summary>
    /// Single HUD slot widget that displays icon/cooldown and forwards click callbacks.
    /// Slot index is assigned by the parent view at runtime.
    /// </summary>
    public sealed class AbilityHudSlotWidget : MonoBehaviour
    {
        [SerializeField] private Button _triggerButton;
        [SerializeField] private Image _iconImage;
        [SerializeField] private Image _cooldownFill;
        [SerializeField] private TMP_Text _cooldownLabel;

        private System.Action<int> _triggerCallback;
        private int _slotIndex = -1;

        private void Awake()
        {
            if (_triggerButton != null)
            {
                _triggerButton.onClick.RemoveAllListeners();
                _triggerButton.onClick.AddListener(OnClicked);
            }
        }

        public void SetSlotIndex(int slotIndex)
        {
            _slotIndex = slotIndex;
        }

        public void SetTriggerCallback(System.Action<int> triggerCallback)
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
            if (_slotIndex < 0)
            {
                return;
            }

            _triggerCallback?.Invoke(_slotIndex);
        }
    }
}
