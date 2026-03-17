using System;

namespace CaseStudy.Feature.AbilitySystem.Data
{
    [Flags]
    public enum AbilityModuleExecutionTime
    {
        None = 0,
        BeforeTrigger = 1 << 0,
        BeforeExecute = 1 << 1,
        AfterExecute = 1 << 2,
        ExecutorRuntime = 1 << 3,
        PresentationSetup = 1 << 4
    }
}
