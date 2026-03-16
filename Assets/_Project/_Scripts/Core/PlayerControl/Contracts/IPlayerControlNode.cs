namespace CaseStudy.Core.PlayerControl.Contracts
{
    /// <summary>
    /// Represents a controllable player entry in the switch service.
    /// </summary>
    public interface IPlayerControlNode
    {
        bool IsAvailable { get; }

        void SetControlState(bool isControlled);
    }
}
