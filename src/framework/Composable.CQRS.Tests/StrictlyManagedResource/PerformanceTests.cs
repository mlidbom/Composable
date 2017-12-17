using Composable.System;
using Composable.Testing;
using Composable.Testing.Performance;
using NUnit.Framework;

namespace Composable.Tests.StrictlyManagedResource
{
    [TestFixture, Performance]
    public class PerformanceTests
    {
        // ReSharper disable once ClassNeverInstantiated.Local
        class StrictResource : IStrictlyManagedResource
        {
            public void Dispose() { }
        }

        [Test] public void Allocated_and_disposes_250_instances_in_10_millisecond_when_actually_collecting_stack_traces()
        {
            TimeAsserter.Execute(() => new StrictlyManagedResource<StrictResource>(forceStackTraceCollection: true).Dispose(),
                                 iterations: 250,
                                 maxTotal: 10.Milliseconds()
                                             .AdjustRuntimeToTestEnvironment(),
                                 timeFormat: "s\\.ffffff");
        }

        [Test] public void Allocates_and_disposes_5000_instances_in_10_millisecond_when_not_collecting_stack_traces()
        {
            TimeAsserter.Execute(() => new StrictlyManagedResource<StrictResource>().Dispose(),
                                 iterations: 5000,
                                 maxTotal: 10.Milliseconds(),
                                 timeFormat: "s\\.ffffff");
        }
    }
}
