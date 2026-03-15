using CaseStudy.Shared.Pooling;

namespace CaseStudy.Feature.AbilitySystem.Abilities
{
    public sealed class ProjectilePool
    {
        private readonly ComponentPool<ProjectileRuntime> _pool;

        public bool IsValid => _pool != null && _pool.IsValid;

        public ProjectilePool(ProjectileRuntime projectilePrefab, int maxPoolSize)
        {
            _pool = new ComponentPool<ProjectileRuntime>(
                projectilePrefab,
                $"ProjectilePool_{projectilePrefab.name}",
                PoolRootRegistry.GetPoolsRoot(),
                maxPoolSize);
        }

        public ProjectileRuntime Get()
        {
            return _pool.GetOrCreate();
        }

        public void Release(ProjectileRuntime projectile)
        {
            if (projectile == null)
            {
                return;
            }

            _pool.Release(projectile);
        }
    }
}


