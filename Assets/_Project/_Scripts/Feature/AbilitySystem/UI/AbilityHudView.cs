using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CaseStudy.Feature.AbilitySystem.UI
{
    /// <summary>
    /// Scene HUD implementation for ability slots, cooldowns, and energy.
    /// Slot index is defined by the widget array order.
    /// </summary>
    public sealed class AbilityHudView : MonoBehaviour
    {
        [Header("Energy")]
        [SerializeField] private Slider _energySlider;
        [SerializeField] private TMP_Text _energyLabel;

        [Header("Slot Widgets")]
        [SerializeField] private AbilityHudSlotWidget[] _slotWidgets;

        public void SetTriggerCallback(System.Action<int> triggerCallback)
        {
            if (_slotWidgets == null)
            {
                return;
            }

            int count = _slotWidgets.Length;
            for (int i = 0; i < count; i++)
            {
                AbilityHudSlotWidget widget = _slotWidgets[i];
                if (widget == null)
                {
                    continue;
                }

                widget.SetSlotIndex(i);
                widget.SetTriggerCallback(triggerCallback);
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

        public void BindSlot(int slotIndex, Sprite icon)
        {
            AbilityHudSlotWidget widget = GetWidgetByIndex(slotIndex);
            if (widget == null)
            {
                return;
            }

            widget.BindIcon(icon);
            widget.ClearCooldown();
        }

        public void SetCooldown(int slotIndex, float remainingSeconds, float normalizedRemaining)
        {
            AbilityHudSlotWidget widget = GetWidgetByIndex(slotIndex);
            if (widget == null)
            {
                return;
            }

            widget.SetCooldown(remainingSeconds, normalizedRemaining);
        }

        public void ClearCooldown(int slotIndex)
        {
            AbilityHudSlotWidget widget = GetWidgetByIndex(slotIndex);
            if (widget == null)
            {
                return;
            }

            widget.ClearCooldown();
        }

        private AbilityHudSlotWidget GetWidgetByIndex(int slotIndex)
        {
            if (_slotWidgets == null || slotIndex < 0 || slotIndex >= _slotWidgets.Length)
            {
                return null;
            }

            return _slotWidgets[slotIndex];
        }
    }
}
