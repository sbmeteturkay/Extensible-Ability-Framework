using System.Threading;
using CaseStudy.Feature.AbilitySystem.Domain;
using CaseStudy.Shared.AbilitySystem.Events;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CaseStudy.Feature.AbilitySystem.Data
{
    /// <summary>
    /// Optional, executor-independent addon data.
    /// Modules are composable and can be shared by many abilities.
    /// </summary>
    public abstract class AbilityOptionalModuleSO : ScriptableObject
    {
        [SerializeField] private int _order;

        public int Order => _order;

        public virtual bool IsCompatibleWith(AbilityExecutorSO executor)
        {
            return true;
        }

        public virtual bool TryApplyBeforeTrigger(
            AbilityContext context,
            AbilityDataSO data,
            ref AbilityExecutionOptions options,
            out AbilityFailureReason failureReason,
            out string failureMessage)
        {
            failureReason = AbilityFailureReason.None;
            failureMessage = string.Empty;
            return true;
        }

        public virtual UniTask OnBeforeExecuteAsync(
            AbilityContext context,
            AbilityDataSO data,
            AbilityExecutionOptions options,
            CancellationToken cancellationToken)
        {
            return UniTask.CompletedTask;
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
