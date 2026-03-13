using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CaseStudy.Feature.AbilitySystem.UI
{
    /// <summary>
    /// Scene HUD implementation for ability slots, cooldowns, and energy.
    /// </summary>
    public sealed class AbilityHudView : MonoBehaviour
    {
        [Header("Energy")]
        [SerializeField] private Slider _energySlider;
        [SerializeField] private TMP_Text _energyLabel;

        [Header("Slot Widgets")]
        [SerializeField] private AbilityHudSlotWidget[] _slotWidgets;

        public void SetTriggerCallback(System.Action<string> triggerCallback)
        {
            if (_slotWidgets == null)
            {
                return;
            }

            int count = _slotWidgets.Length;
            for (int i = 0; i < count; i++)
            {
                AbilityHudSlotWidget widget = _slotWidgets[i];
                if (widget != null)
                {
                    widget.SetTriggerCallback(triggerCallback);
                }
            }
        }

        public void SetEnergy(float currentEnergy, float maxEnergy)
        {
            float safeMax = Mathf.Max(0.0001f, maxEnergy);
            float safeCurrent = Mathf.Clamp(currentEnergy, 0f, safeMax);

            if (_energySlider != null)
            {
                _energySlider.maxValue = safeMax;
                _energySlider.value = safeCurrent;
            }

            if (_energyLabel != null)
            {
                _energyLabel.text = $"{safeCurrent:0}/{safeMax:0}";
            }
        }

        public void BindSlot(string slotKey, Sprite icon)
        {
            AbilityHudSlotWidget widget = FindWidget(slotKey);
            if (widget == null)
            {
                return;
            }

            widget.BindIcon(icon);
            widget.ClearCooldown();
        }

        public void SetCooldown(string slotKey, float remainingSeconds, float normalizedRemaining)
        {
            AbilityHudSlotWidget widget = FindWidget(slotKey);
            if (widget == null)
            {
                return;
            }

            widget.SetCooldown(remainingSeconds, normalizedRemaining);
        }

        public void ClearCooldown(string slotKey)
        {
            AbilityHudSlotWidget widget = FindWidget(slotKey);
            if (widget == null)
            {
                return;
            }

            widget.ClearCooldown();
        }

        private AbilityHudSlotWidget FindWidget(string slotKey)
        {
            slotKey = NormalizeKey(slotKey);
            if (_slotWidgets == null || string.IsNullOrWhiteSpace(slotKey))
            {
                return null;
            }

            int count = _slotWidgets.Length;
            for (int i = 0; i < count; i++)
            {
                AbilityHudSlotWidget widget = _slotWidgets[i];
                if (widget == null)
                {
                    continue;
                }

                string widgetSlotKey = NormalizeKey(widget.SlotKey);
                if (widgetSlotKey == slotKey)
                {
                    return widget;
                }
            }

            return null;
        }

        private static string NormalizeKey(string key)
        {
            return string.IsNullOrWhiteSpace(key) ? string.Empty : key.Trim();
        }
    }
}
