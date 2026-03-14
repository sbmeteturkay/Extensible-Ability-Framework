using System.Threading;
using CaseStudy.Feature.AbilitySystem.Domain;
using CaseStudy.Shared.AbilitySystem.Events;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CaseStudy.Feature.AbilitySystem.Data
{
    /// <summary>
    /// Base override extension point for ability execution flow.
    /// Use this to customize runtime options or block trigger logic without changing core ability code.
    /// </summary>
    public abstract class AbilityOverrideSO : ScriptableObject
    {
        [SerializeField] private int _order;

        public int Order => _order;

        public virtual bool TryApplyBeforeTrigger(
            AbilityContext context,
            AbilityDataSO data,
            ref AbilityExecutionOptions options,
            out AbilityFailureReason failureReason)
        {
            failureReason = AbilityFailureReason.None;
            return true;
        }

        public virtual UniTask OnAfterExecuteAsync(
            AbilityContext context,
            AbilityDataSO data,
            AbilityExecutionOptions options,
            CancellationToken cancellationToken)
        {
            return UniTask.CompletedTask;
        }
    }
}
