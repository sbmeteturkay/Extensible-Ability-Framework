using CaseStudy.Feature.AbilitySystem.Contracts;
using CaseStudy.Shared.AbilitySystem.Events;
using MessagePipe;
using UnityEngine;

namespace CaseStudy.Feature.AbilitySystem.Services
{
    public sealed class EnergyService : IEnergyService
    {
        private const float DEFAULT_MAX_ENERGY = 100f;

        private readonly IPublisher<AbilityEnergyChangedEvent> _energyChangedPublisher;
        private float _currentEnergy = DEFAULT_MAX_ENERGY;

        public EnergyService(IPublisher<AbilityEnergyChangedEvent> energyChangedPublisher)
        {
            _energyChangedPublisher = energyChangedPublisher;
            PublishEnergyChanged();
        }

        public float CurrentEnergy => _currentEnergy;

        public float MaxEnergy => DEFAULT_MAX_ENERGY;

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