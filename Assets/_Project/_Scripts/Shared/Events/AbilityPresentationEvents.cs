using UnityEngine;

namespace CaseStudy.Shared.AbilitySystem.Events.Presentation
{
    public readonly struct AbilityLoadoutSlotAssignedEvent
    {
        public AbilityLoadoutSlotAssignedEvent(int slotIndex, string abilityKey, Sprite icon)
            : this(slotIndex, abilityKey, icon, null, 1f)
        {
        }

        public AbilityLoadoutSlotAssignedEvent(int slotIndex, string abilityKey, Sprite icon, AnimationClip abilityAnimationClip)
            : this(slotIndex, abilityKey, icon, abilityAnimationClip, 1f)
        {
        }

        public AbilityLoadoutSlotAssignedEvent(int slotIndex, string abilityKey, Sprite icon, AnimationClip abilityAnimationClip, float abilityAnimationSpeed)
        {
            SlotIndex = slotIndex;
            AbilityKey = abilityKey;
            Icon = icon;
            AbilityAnimationClip = abilityAnimationClip;
            AbilityAnimationSpeed = abilityAnimationSpeed;
        }

        public int SlotIndex { get; }
        public string AbilityKey { get; }
        public Sprite Icon { get; }
        public AnimationClip AbilityAnimationClip { get; }
        public float AbilityAnimationSpeed { get; }
    }
}
