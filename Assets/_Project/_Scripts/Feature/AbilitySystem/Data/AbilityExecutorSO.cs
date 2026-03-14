using System.Threading;
using CaseStudy.Feature.AbilitySystem.Domain;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CaseStudy.Feature.AbilitySystem.Data
{
    /// <summary>
    /// Data-driven ability logic entry point.
    /// Same executor class can power many ability assets with different values.
    /// </summary>
    public abstract class AbilityExecutorSO : ScriptableObject
    {
        public virtual bool CanExecute(AbilityContext context, AbilityDataSO data)
        {
            return context != null && data != null;
        }

        public abstract UniTask ExecuteAsync(AbilityContext context, AbilityDataSO data, CancellationToken cancellationToken);

        public virtual void Tick(AbilityContext context, AbilityDataSO data, float deltaTime)
        {
        }

        public virtual bool TryValidate(AbilityDataSO data, out string validationError)
        {
            validationError = string.Empty;
            return true;
        }
    }
}
