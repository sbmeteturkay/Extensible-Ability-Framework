using UnityEngine;

namespace CaseStudy.Feature.Locomotion.Contracts
{
    public interface ILocomotionInputReader
    {
        Vector2 MoveInput { get; }
    }
}
