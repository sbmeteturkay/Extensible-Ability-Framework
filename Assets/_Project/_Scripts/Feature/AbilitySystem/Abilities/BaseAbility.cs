using System;
using System.Threading;
using CaseStudy.Feature.AbilitySystem.Contracts;
using CaseStudy.Feature.AbilitySystem.Data;
using CaseStudy.Feature.AbilitySystem.Domain;
using Cysharp.Threading.Tasks;

namespace CaseStudy.Feature.AbilitySystem.Abilities
{
    /// <summary>
    /// Shared ability base class that stores context/data and common lifecycle defaults.
    /// </summary>
    public abstract class BaseAbility : IAbility
    {
        protected AbilityContext Context { get; private set; }

        protected AbilityDataSO Data { get; private set; }

        public string AbilityKey => Data != null ? Data.AbilityKey : string.Empty;

        public virtual void Initialize(AbilityContext context, AbilityDataSO data)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            Data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public virtual bool CanExecute()
        {
            return Context != null && Data != null && !string.IsNullOrWhiteSpace(AbilityKey);
        }

        public virtual void Tick(float deltaTime)
        {
        }

        public abstract UniTask ExecuteAsync(CancellationToken cancellationToken);
    }
}
