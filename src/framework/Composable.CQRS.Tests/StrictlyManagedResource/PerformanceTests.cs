using Composable.System;
using Composable.Testing.Performance;
using NCrunch.Framework;
using NUnit.Framework;
// ReSharper disable StringLiteralTypo

namespace Composable.Tests.StrictlyManagedResource
{
    [TestFixture, Performance, Serial]
    public class PerformanceTests
    {
        // ReSharper disable once ClassNeverInstantiated.Local
        class StrictResource : IStrictlyManagedResource
        {
            public void Dispose() { }
        }

        [Test] public void Allocated_and_disposes_250_instances_in_20_millisecond_when_actually_collecting_stack_traces()
        {
            TimeAsserter.Execute(() => new StrictlyManagedResource<StrictResource>(forceStackTraceCollection: true).Dispose(),
                                 iterations: 250,
                                 maxTotal: 30.Milliseconds());
        }

        [Test] public void Allocates_and_disposes_5000_instances_in_10_millisecond_when_not_collecting_stack_traces()
        {
            TimeAsserter.Execute(() => new StrictlyManagedResource<StrictResource>().Dispose(),
                                 iterations: 5000,
                                 maxTotal: 10.Milliseconds());
        }
    }
}
