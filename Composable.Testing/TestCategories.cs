using System;
using System.Collections.Generic;

namespace Composable.Testing
{
    [AttributeUsage(AttributeTargets.Class)]
    class PerformanceAttribute : NCrunch.Framework.ExclusivelyUsesAttribute,
                                 NUnit.Framework.Interfaces.IApplyToTest,
                                 NUnit.Framework.Interfaces.IApplyToContext
    {
        public PerformanceAttribute(ResourceType resourceType = ResourceType.Cpu | ResourceType.Disk) : base(GetResourceTypes(resourceType)) {}
        static string[] GetResourceTypes(ResourceType resourceType)
        {
            var typeNames = new List<string>();
            if(resourceType.HasFlag(ResourceType.Cpu))
            {
                typeNames.Add("CpuPerformance");
            }
            if(resourceType.HasFlag(ResourceType.Disk))
            {
                typeNames.Add("DiskPerformance");
            }
            return typeNames.ToArray();
        }

        //Add the NUnit category performance
        public void ApplyToTest(NUnit.Framework.Internal.Test test)
            => test.Properties.Add("Category", "Performance");

        //Tell NUnit test runners not to run this test in parallel with others.
        public void ApplyToContext(NUnit.Framework.Internal.TestExecutionContext context)
            => context.ParallelScope = NUnit.Framework.ParallelScope.None;
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)] class LongRunningAttribute : Attribute,
                                                                                                    NUnit.Framework.Interfaces.IApplyToTest
    {
        public void ApplyToTest(NUnit.Framework.Internal.Test test)
            => test.Properties.Add("Category", "LongRunning");
    }

    [Flags]
    enum ResourceType
    {
        Cpu = 1,
        Disk = 2
    }
}

