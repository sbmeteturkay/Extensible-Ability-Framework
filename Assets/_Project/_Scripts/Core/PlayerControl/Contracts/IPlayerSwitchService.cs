namespace CaseStudy.Core.PlayerControl.Contracts
{
    /// <summary>
    /// Coordinates which player currently receives control.
    /// </summary>
    public interface IPlayerSwitchService
    {
        void Register(IPlayerControlNode player);

        void Unregister(IPlayerControlNode player);

        bool RequestSwitch(IPlayerControlNode requester);
    }
}
