using System;
using System.Threading;
using CaseStudy.Feature.AbilitySystem.Contracts;
using CaseStudy.Feature.AbilitySystem.Data;
using Cysharp.Threading.Tasks;

namespace CaseStudy.Feature.AbilitySystem.Abilities
{
    /// <summary>
    /// Generic runtime ability that delegates behavior to a data-driven executor asset.
    /// </summary>
    public sealed class ExecutorAbility : BaseAbility
    {
        private readonly AbilityExecutorSO _executor;

        public ExecutorAbility(AbilityExecutorSO executor)
        {
            _executor = executor;
        }

        public override bool CanExecute()
        {
            return base.CanExecute()
                   && _executor != null
                   && _executor.CanExecute(Context, Data);
        }

        public override UniTask ExecuteAsync(CancellationToken cancellationToken)
        {
            if (_executor == null)
            {
                throw new InvalidOperationException("ExecutorAbility requires an executor reference.");
            }

            return _executor.ExecuteAsync(Context, Data, cancellationToken);
        }

        public override void Tick(float deltaTime)
        {
            if (_executor == null)
            {
                return;
            }

            _executor.Tick(Context, Data, deltaTime);
        }
    }
}
