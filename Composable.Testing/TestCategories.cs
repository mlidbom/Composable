using System;

namespace Composable.Testing
{
    [AttributeUsage(AttributeTargets.Class)]
    class PerformanceAttribute : NCrunch.Framework.ExclusivelyUsesAttribute,
                                 NUnit.Framework.Interfaces.IApplyToTest,
                                 NUnit.Framework.Interfaces.IApplyToContext
    {
        //Exclusively use the resource: "Performance"
        public PerformanceAttribute() : base("Performance") {}

        //Add the NUnit category performance
        public void ApplyToTest(NUnit.Framework.Internal.Test test)
            => test.Properties.Add("Category", "Performance");

        //Tell NUnit test runners not to run this test in parallel with others.
        public void ApplyToContext(NUnit.Framework.Internal.TestExecutionContext context)
            => context.ParallelScope = NUnit.Framework.ParallelScope.None;
    }
}
