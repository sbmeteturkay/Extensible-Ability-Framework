using System.Collections.Generic;
using CaseStudy.Feature.AbilitySystem.Contracts;
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

        private readonly Dictionary<string, CooldownState> _states = new();
        private readonly List<string> _completedBuffer = new(8);
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

            foreach (KeyValuePair<string, CooldownState> pair in _states)
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
                string abilityKey = _completedBuffer[i];
                _states.Remove(abilityKey);
                _cooldownCompletedPublisher.Publish(new AbilityCooldownCompletedEvent(abilityKey));
            }
        }

        public bool IsReady(string abilityKey)
        {
            if (string.IsNullOrWhiteSpace(abilityKey))
            {
                return false;
            }

            return GetRemaining(abilityKey) <= 0f;
        }

        public void StartCooldown(string abilityKey, float durationSeconds)
        {
            if (string.IsNullOrWhiteSpace(abilityKey))
            {
                return;
            }

            float duration = Mathf.Max(0f, durationSeconds);

            if (duration <= 0f)
            {
                _states.Remove(abilityKey);
                _cooldownCompletedPublisher.Publish(new AbilityCooldownCompletedEvent(abilityKey));
                return;
            }

            float endTime = Time.time + duration;
            _states[abilityKey] = new CooldownState(endTime, duration);
            _cooldownStartedPublisher.Publish(new AbilityCooldownStartedEvent(abilityKey, duration));
        }

        public float GetRemaining(string abilityKey)
        {
            if (string.IsNullOrWhiteSpace(abilityKey))
            {
                return 0f;
            }

            if (!_states.TryGetValue(abilityKey, out CooldownState state))
            {
                return 0f;
            }

            float remaining = state.EndTime - Time.time;
            if (remaining <= 0f)
            {
                _states.Remove(abilityKey);
                _cooldownCompletedPublisher.Publish(new AbilityCooldownCompletedEvent(abilityKey));
                return 0f;
            }

            return remaining;
        }
    }
}
