using CaseStudy.Feature.AbilitySystem.Contracts;
using CaseStudy.Shared.AbilitySystem.Events;
using MessagePipe;
using UnityEngine;
using VContainer.Unity;

namespace CaseStudy.Feature.AbilitySystem.Services
{
    /// <summary>
    /// Maintains ability energy state, publishes energy changes, and applies passive regeneration.
    /// </summary>
    public sealed class EnergyService : IEnergyService, ITickable
    {
        private const float DEFAULT_MAX_ENERGY = 100f;
        private const float REGEN_PER_SECOND = 10f;

        private readonly IPublisher<AbilityEnergyChangedEvent> _energyChangedPublisher;
        private float _currentEnergy = DEFAULT_MAX_ENERGY;

        public EnergyService(IPublisher<AbilityEnergyChangedEvent> energyChangedPublisher)
        {
            _energyChangedPublisher = energyChangedPublisher;
            PublishEnergyChanged();
        }

        public float CurrentEnergy => _currentEnergy;

        public float MaxEnergy => DEFAULT_MAX_ENERGY;

        public void Tick()
        {
            if (_currentEnergy >= MaxEnergy)
            {
                return;
            }

            float nextEnergy = Mathf.Min(MaxEnergy, _currentEnergy + (REGEN_PER_SECOND * Time.deltaTime));
            if (Mathf.Approximately(nextEnergy, _currentEnergy))
            {
                return;
            }

            _currentEnergy = nextEnergy;
            PublishEnergyChanged();
        }

        public bool HasEnough(float cost)
        {
            return _currentEnergy >= Mathf.Max(0f, cost);
        }

        public bool TryConsume(float cost)
        {
            float safeCost = Mathf.Max(0f, cost);

            if (_currentEnergy < safeCost)
            {
                return false;
            }

            _currentEnergy -= safeCost;
            PublishEnergyChanged();
            return true;
        }

        public void Restore(float amount)
        {
            float safeAmount = Mathf.Max(0f, amount);
            _currentEnergy = Mathf.Min(MaxEnergy, _currentEnergy + safeAmount);
            PublishEnergyChanged();
        }

        private void PublishEnergyChanged()
        {
            _energyChangedPublisher.Publish(new AbilityEnergyChangedEvent(_currentEnergy, MaxEnergy));
        }
    }
}