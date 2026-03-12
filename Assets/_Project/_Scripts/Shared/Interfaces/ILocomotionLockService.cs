namespace CaseStudy.Shared.Locomotion.Interfaces
{
    public interface ILocomotionLockService
    {
        bool IsLocked { get; }

        void PushLock();

        void PopLock();
    }
}
