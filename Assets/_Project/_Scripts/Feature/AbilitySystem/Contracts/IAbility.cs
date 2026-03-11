using System.Threading;
using CaseStudy.Feature.AbilitySystem.Data;
using CaseStudy.Feature.AbilitySystem.Domain;
using Cysharp.Threading.Tasks;

namespace CaseStudy.Feature.AbilitySystem.Contracts
{
    public interface IAbility
    {
        AbilityId Id { get; }

        void Initialize(AbilityContext context, AbilityDataSO data);

        bool CanExecute();

        UniTask ExecuteAsync(CancellationToken cancellationToken);

        void Tick(float deltaTime);
    }
}