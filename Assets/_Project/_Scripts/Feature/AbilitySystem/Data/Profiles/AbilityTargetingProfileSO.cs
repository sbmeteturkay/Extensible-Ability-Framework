using UnityEngine;

namespace CaseStudy.Feature.AbilitySystem.Data
{
    [CreateAssetMenu(fileName = "SO_AbilityTargetingProfile", menuName = "Ability/Targeting Profile")]
    public sealed class AbilityTargetingProfileSO : ScriptableObject
    {
        [Header("Layer Mapping")]
        [SerializeField] private LayerMask _hitObstacles;
        [SerializeField] private LayerMask _hitPlayer;
        [SerializeField] private LayerMask _hitEnemies;
        [SerializeField] private LayerMask _hitAllies;

        public LayerMask Resolve(AbilityTargetGroups groups)
        {
            int mask = 0;

            if ((groups & AbilityTargetGroups.HitObstacles) != 0)
            {
                mask |= _hitObstacles.value;
            }

            if ((groups & AbilityTargetGroups.HitPlayer) != 0)
            {
                mask |= _hitPlayer.value;
            }

            if ((groups & AbilityTargetGroups.HitEnemies) != 0)
            {
                mask |= _hitEnemies.value;
            }

            if ((groups & AbilityTargetGroups.HitAllies) != 0)
            {
                mask |= _hitAllies.value;
            }

            LayerMask resolved = default;
            resolved.value = mask;
            return resolved;
        }
    }
}
