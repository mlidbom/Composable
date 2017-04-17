using System;

namespace Composable.Testing
{
    [AttributeUsage(AttributeTargets.Class)]
    class PerformanceAttribute : Attribute,
                                 NUnit.Framework.Interfaces.IApplyToTest
    {
        //Add the NUnit category performance
        public void ApplyToTest(NUnit.Framework.Internal.Test test)
            => test.Properties.Add("Category", "Performance");
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)] class LongRunningAttribute : Attribute,
                                                                                                    NUnit.Framework.Interfaces.IApplyToTest
    {
        public void ApplyToTest(NUnit.Framework.Internal.Test test)
            => test.Properties.Add("Category", "LongRunning");
    }
}

