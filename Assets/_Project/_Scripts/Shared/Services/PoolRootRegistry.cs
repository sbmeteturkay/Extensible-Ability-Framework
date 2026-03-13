using UnityEngine;

namespace CaseStudy.Shared.Pooling
{
    internal static class PoolRootRegistry
    {
        private static Transform _abilityPoolsRoot;

        public static Transform GetAbilityPoolsRoot()
        {
            if (_abilityPoolsRoot != null)
            {
                return _abilityPoolsRoot;
            }

            GameObject rootObject = new GameObject("AbilityPools_Root");
            _abilityPoolsRoot = rootObject.transform;
            return _abilityPoolsRoot;
        }
    }
}
