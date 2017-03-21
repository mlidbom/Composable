namespace Composable.Testing
{
    class PerformanceAttribute : NCrunch.Framework.ExclusivelyUsesAttribute,
                                 NUnit.Framework.Interfaces.IApplyToTest,
                                 NUnit.Framework.Interfaces.IApplyToContext
    {
        public PerformanceAttribute() : base("Performance") {}

        public void ApplyToTest(NUnit.Framework.Internal.Test test)
            => test.Properties.Add("Category", "Performance");

        public void ApplyToContext(NUnit.Framework.Internal.TestExecutionContext context)
            => context.ParallelScope = NUnit.Framework.ParallelScope.None;
    }
}
