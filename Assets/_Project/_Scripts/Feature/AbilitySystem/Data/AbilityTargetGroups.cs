using System;

namespace CaseStudy.Feature.AbilitySystem.Data
{
    [Flags]
    public enum AbilityTargetGroups
    {
        None = 0,
        HitObstacles = 1 << 0,
        HitPlayer = 1 << 1,
        HitEnemies = 1 << 2,
        HitAllies = 1 << 3
    }
}
