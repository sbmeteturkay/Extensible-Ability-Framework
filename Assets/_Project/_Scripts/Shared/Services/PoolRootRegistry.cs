using UnityEngine;

namespace CaseStudy.Shared.Pooling
{
    public static class PoolRootRegistry
    {
        private static Transform _poolsRoot;

        public static Transform GetPoolsRoot()
        {
            if (_poolsRoot != null)
            {
                return _poolsRoot;
            }

            GameObject rootObject = new GameObject("Pools_Root");
            _poolsRoot = rootObject.transform;
            return _poolsRoot;
        }

        [System.Obsolete("Use GetPoolsRoot().")]
        public static Transform GetAbilityPoolsRoot()
        {
            return GetPoolsRoot();
        }
    }
}
