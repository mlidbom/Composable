using System;
using Composable.System;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.StrictlyManagedResource
{
    [TestFixture] public class When_no_gc_root_exists_for_object_and_garbage_collector_runs_and_instance_is_not_disposed
    {
        class StrictResource : IStrictlyManagedResource
        {
            public void Dispose() { }
        }

        [SetUp] public void SetupTask() { }

        StrictlyManagedResourceWasFinalizedException LeakResourceAndRunGarbageCollection(bool forceStackTraceCollection)
        {
            StrictlyManagedResourceWasFinalizedException exception = null;
            StrictlyManagedResource<StrictResource>.ThrowCreatedExceptionWhenFinalizerIsCalled = resource => exception = resource;

            ((Action)(() => new StrictlyManagedResource<StrictResource>(forceStackTraceCollection: forceStackTraceCollection))).Invoke();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            return exception;
        }

        [Test] public void Lead_is_detected() =>
            LeakResourceAndRunGarbageCollection(forceStackTraceCollection: false)
                .Should()
                .NotBeNull();

        [Test] public void And_stack_trace_collection_is_enabled_exception_messages_contains_name_of_allocating_class_and_method_and_managed_type() =>
            LeakResourceAndRunGarbageCollection(forceStackTraceCollection: true)
                .Message
                .Should()
                .NotBeNullOrEmpty()
                .And
                .Contain(nameof(When_no_gc_root_exists_for_object_and_garbage_collector_runs_and_instance_is_not_disposed))
                .And
                .Contain(nameof(And_stack_trace_collection_is_enabled_exception_messages_contains_name_of_allocating_class_and_method_and_managed_type))
                .And
               .Contain(nameof(StrictResource));
    }
}
