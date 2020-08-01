using System;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Composable.Testing
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class PerformanceAttribute : PropertyAttribute,
                                 NUnit.Framework.Interfaces.IApplyToContext
    {
        public PerformanceAttribute()
        {
            Properties.Set(PropertyNames.ParallelScope, ParallelScope.None);
            Properties.Add(PropertyNames.Category, "Performance");
        }

        //Add the NUnit category performance
        public override void ApplyToTest(Test test)
        {
            test.Properties.Add(PropertyNames.Category, "Performance");
#if !NCRUNCH
            var parallel = new ParallelizableAttribute(ParallelScope.None);
            parallel.ApplyToTest(test);
#endif
        }

        public void ApplyToContext(TestExecutionContext context)
        {
#if !NCRUNCH
            new ParallelizableAttribute(ParallelScope.None).ApplyToContext(context);
#endif
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class LongRunningAttribute : Attribute,
                                        NUnit.Framework.Interfaces.IApplyToTest
    {
        public void ApplyToTest(Test test)
            => test.Properties.Add(PropertyNames.Category, "LongRunning");
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Assembly)]
    public class LevelOfParallelismCEAttribute : Attribute,
                                                 NUnit.Framework.Interfaces.IApplyToTest
    {
        public void ApplyToTest(Test test)
            => test.Properties.Add(PropertyNames.LevelOfParallelism, Math.Max(Environment.ProcessorCount / 3, 4)); //Math.Max(Environment.ProcessorCount / 3, 4)
    }
}
