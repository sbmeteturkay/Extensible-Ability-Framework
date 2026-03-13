using CaseStudy.Shared.Locomotion.Interfaces;
using UnityEngine;

namespace CaseStudy.Shared.Locomotion.Services
{
    /// <summary>
    /// Shared per-player locomotion lock state.
    /// Registered in PlayerLifetimeScope so sibling feature scopes use the same lock counter.
    /// </summary>
    public sealed class LocomotionLockService : ILocomotionLockService
    {
        private int _lockCount;

        public bool IsLocked => _lockCount > 0;

        public void PushLock()
        {
            _lockCount++;
        }

        public void PopLock()
        {
            if (_lockCount <= 0)
            {
                _lockCount = 0;
                return;
            }

            _lockCount--;
        }
    }
}
