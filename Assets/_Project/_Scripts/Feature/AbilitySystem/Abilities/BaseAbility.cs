using System;
using System.Threading;
using CaseStudy.Feature.AbilitySystem.Contracts;
using CaseStudy.Feature.AbilitySystem.Data;
using CaseStudy.Feature.AbilitySystem.Domain;
using Cysharp.Threading.Tasks;

namespace CaseStudy.Feature.AbilitySystem.Abilities
{
    public abstract class BaseAbility : IAbility
    {
        protected AbilityContext Context { get; private set; }

        protected AbilityDataSO Data { get; private set; }

        public AbilityId Id => Data != null ? Data.AbilityId : AbilityId.None;

        public virtual void Initialize(AbilityContext context, AbilityDataSO data)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            Data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public virtual bool CanExecute()
        {
            return Context != null && Data != null && Id != AbilityId.None;
        }

        public virtual void Tick(float deltaTime)
        {
        }

        public abstract UniTask ExecuteAsync(CancellationToken cancellationToken);
    }
}