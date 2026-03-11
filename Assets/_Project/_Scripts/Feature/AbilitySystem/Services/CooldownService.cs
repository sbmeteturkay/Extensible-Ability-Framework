using System.Collections.Generic;
using CaseStudy.Feature.AbilitySystem.Contracts;
using CaseStudy.Feature.AbilitySystem.Domain;
using CaseStudy.Shared.AbilitySystem.Events;
using MessagePipe;
using UnityEngine;

namespace CaseStudy.Feature.AbilitySystem.Services
{
    /// <summary>
    /// Tracks cooldown timers per ability and publishes cooldown lifecycle events.
    /// </summary>
    public sealed class CooldownService : ICooldownService
    {
        private readonly struct CooldownState
        {
            public CooldownState(float endTime, float duration)
            {
                EndTime = endTime;
                Duration = duration;
            }

            public float EndTime { get; }

            public float Duration { get; }
        }

        private readonly Dictionary<AbilityId, CooldownState> _states = new();
        private readonly IPublisher<AbilityCooldownStartedEvent> _cooldownStartedPublisher;
        private readonly IPublisher<AbilityCooldownUpdatedEvent> _cooldownUpdatedPublisher;
        private readonly IPublisher<AbilityCooldownCompletedEvent> _cooldownCompletedPublisher;

        public CooldownService(
            IPublisher<AbilityCooldownStartedEvent> cooldownStartedPublisher,
            IPublisher<AbilityCooldownUpdatedEvent> cooldownUpdatedPublisher,
            IPublisher<AbilityCooldownCompletedEvent> cooldownCompletedPublisher)
        {
            _cooldownStartedPublisher = cooldownStartedPublisher;
            _cooldownUpdatedPublisher = cooldownUpdatedPublisher;
            _cooldownCompletedPublisher = cooldownCompletedPublisher;
        }

        public bool IsReady(AbilityId abilityId)
        {
            return GetRemaining(abilityId) <= 0f;
        }

        public void StartCooldown(AbilityId abilityId, float durationSeconds)
        {
            float duration = Mathf.Max(0f, durationSeconds);

            if (duration <= 0f)
            {
                _states.Remove(abilityId);
                _cooldownCompletedPublisher.Publish(new AbilityCooldownCompletedEvent(abilityId));
                return;
            }

            float endTime = Time.time + duration;
            _states[abilityId] = new CooldownState(endTime, duration);
            _cooldownStartedPublisher.Publish(new AbilityCooldownStartedEvent(abilityId, duration));
        }

        public float GetRemaining(AbilityId abilityId)
        {
            if (!_states.TryGetValue(abilityId, out CooldownState state))
            {
                return 0f;
            }

            float remaining = state.EndTime - Time.time;

            if (remaining <= 0f)
            {
                _states.Remove(abilityId);
                _cooldownCompletedPublisher.Publish(new AbilityCooldownCompletedEvent(abilityId));
                return 0f;
            }

            float normalizedRemaining = state.Duration <= 0f
                ? 0f
                : Mathf.Clamp01(remaining / state.Duration);

            _cooldownUpdatedPublisher.Publish(new AbilityCooldownUpdatedEvent(abilityId, remaining, normalizedRemaining));
            return remaining;
        }
    }
}