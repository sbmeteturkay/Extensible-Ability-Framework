using System.Collections.Generic;
using CaseStudy.Feature.AbilitySystem.Contracts;
using CaseStudy.Feature.AbilitySystem.Domain;
using CaseStudy.Shared.AbilitySystem.Events;
using MessagePipe;
using UnityEngine;
using VContainer.Unity;

namespace CaseStudy.Feature.AbilitySystem.Services
{
    /// <summary>
    /// Tracks cooldown timers per ability and publishes cooldown lifecycle events.
    /// </summary>
    public sealed class CooldownService : ICooldownService, ITickable
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
        private readonly List<AbilityId> _completedBuffer = new(8);
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

        public void Tick()
        {
            if (_states.Count == 0)
            {
                return;
            }

            _completedBuffer.Clear();
            float now = Time.time;

            foreach (KeyValuePair<AbilityId, CooldownState> pair in _states)
            {
                float remaining = pair.Value.EndTime - now;

                if (remaining <= 0f)
                {
                    _completedBuffer.Add(pair.Key);
                    continue;
                }

                float normalizedRemaining = pair.Value.Duration <= 0f
                    ? 0f
                    : Mathf.Clamp01(remaining / pair.Value.Duration);

                _cooldownUpdatedPublisher.Publish(
                    new AbilityCooldownUpdatedEvent(pair.Key, remaining, normalizedRemaining));
            }

            int completedCount = _completedBuffer.Count;
            for (int i = 0; i < completedCount; i++)
            {
                AbilityId abilityId = _completedBuffer[i];
                _states.Remove(abilityId);
                _cooldownCompletedPublisher.Publish(new AbilityCooldownCompletedEvent(abilityId));
            }
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

            return remaining;
        }
    }
}