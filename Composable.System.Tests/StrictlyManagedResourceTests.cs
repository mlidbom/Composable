using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Composable.System;
using Composable.Testing;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests
{
    [TestFixture] public class StrictlyManagedResourceTests
    {
        class StrictResource : IStrictlyManagedResource
        {
            public void Dispose() { }
        }

        [Test] public void Allocated_and_disposes_40_instances_in_10_millisecond_when_actually_collecting_stack_traces() =>
            TimeAsserter.Execute(() => new StrictlyManagedResource<StrictResource>(forceStackTraceCollection: true).Dispose(),
                                 iterations: 40,
                                 maxTotal: TimeSpanExtensions.Milliseconds(10),
                                 maxTries: 10,
                                 timeFormat: "s\\.ffffff");

        [Test] public void Allocates_and_disposes_7000_instances_in_10_millisecond_when_not_collecting_stack_traces() =>
            TimeAsserter.Execute(() => new StrictlyManagedResource<StrictResource>(forceStackTraceCollection: false).Dispose(),
                                 iterations: 7000,
                                 maxTotal: TimeSpanExtensions.Milliseconds(10),
                                 maxTries: 10,
                                 timeFormat: "s\\.ffffff");

        //todo: Find a way to test this that can run each test run...
        [Test, Ignore("This test should actually fail. But through the appdomain crashing due to the exception in the finalizer.")] public void Throws_exception_if_resource_is_not_disposed()
        {
            UnhandledExceptionEventArgs args = null;
            UnhandledExceptionEventHandler currentDomainOnUnhandledException = (sender, unhandledExceptionEventArgs) => args = unhandledExceptionEventArgs;

            AppDomain.CurrentDomain.UnhandledException += currentDomainOnUnhandledException;

            // ReSharper disable once ObjectCreationAsStatement
            ((Action)(() => new StrictlyManagedResource<StrictResource>())).Invoke();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.WaitForFullGCComplete();

            AppDomain.CurrentDomain.UnhandledException -= currentDomainOnUnhandledException;

            args.ExceptionObject.Should()
                .NotBeNull()
                .And.BeOfType<StrictlyManagedResourceWasFinalizedException>();
        }
    }
}
