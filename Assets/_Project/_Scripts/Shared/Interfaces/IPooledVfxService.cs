using UnityEngine;

namespace CaseStudy.Shared.Vfx.Interfaces
{
    /// <summary>
    /// Spawns VFX instances using pooled objects with optional spawn delay and auto-return.
    /// </summary>
    public interface IPooledVfxService
    {
        void Spawn(GameObject vfxPrefab, Vector3 position, Quaternion rotation, float delaySeconds = 0f, float autoReturnSeconds = 0f);
    }
}
