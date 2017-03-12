using Composable.System;
using Composable.Testing;
using NUnit.Framework;

namespace Composable.Tests.StrictlyManagedResource
{
    public class PerformanceTests
    {
        class StrictResource : IStrictlyManagedResource
        {
            public void Dispose() { }
        }

        [Test] public void Allocated_and_disposes_40_instances_in_10_millisecond_when_actually_collecting_stack_traces()
        {
            TimeAsserter.Execute(() => new StrictlyManagedResource<StrictResource>(forceStackTraceCollection: true).Dispose(),
                                 iterations: 40,
                                 maxTotal: 10.Milliseconds()
                                             .AdjustRuntimeToTestEnvironment(),
                                 maxTries: 10,
                                 timeFormat: "s\\.ffffff");
        }

        [Test] public void Allocates_and_disposes_5000_instances_in_10_millisecond_when_not_collecting_stack_traces()
        {
            TimeAsserter.Execute(() => new StrictlyManagedResource<StrictResource>().Dispose(),
                                 iterations: 5000,
                                 maxTotal: 10.Milliseconds(),
                                 maxTries: 10,
                                 timeFormat: "s\\.ffffff");
        }

        [Test]
        public void Allocates_and_disposes_2000_instances_in_10_millisecond_when_not_collecting_stack_traces_but_tracking_lifetimes()
        {
            TimeAsserter.Execute(() => new StrictlyManagedResource<StrictResource>(forceStackTraceCollection: false, maxLifetime: 1.Minutes()).Dispose(),
                                 iterations: 2000,
                                 maxTotal: 10.Milliseconds()
                                             .AdjustRuntimeToTestEnvironment(),
                                 maxTries: 10,
                                 timeFormat: "s\\.ffffff");
        }
    }
}
