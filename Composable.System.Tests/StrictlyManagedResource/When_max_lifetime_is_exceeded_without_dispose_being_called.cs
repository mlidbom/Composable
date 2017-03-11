using System.Threading;
using Composable.System;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.StrictlyManagedResource
{
    public class When_max_lifetime_is_exceeded_without_dispose_being_called
    {
        class StrictResource : IStrictlyManagedResource
        {
            public void Dispose() { }
        }

        [SetUp] public void SetupTask() { }

        StrictlyManagedResourceLifespanWasExceededException ExceedResourceLifeTimeWithoutDisposing(bool forceStackTraceCollection)
        {
            StrictlyManagedResourceLifespanWasExceededException exception = null;
            var exceptionThrown = new ManualResetEvent(false);
            StrictlyManagedResource<StrictResource>.ThrowCreatedExceptionWhenLifespanWasExceeded = exceptionToThrow =>
                                                                                                   {
                                                                                                       exception = exceptionToThrow;
                                                                                                       exceptionThrown.Set();
                                                                                                   };

            using(new StrictlyManagedResource<StrictResource>(forceStackTraceCollection: forceStackTraceCollection, maxLifetime: TimeSpanExtensions.Milliseconds(10)))
            {
                exceptionThrown.WaitOne(TimeSpanExtensions.Milliseconds(100))
                               .Should()
                               .BeTrue("Timed out waiting for exception to be thrown.");
            }

            return exception;
        }

        [Test] public void Lead_is_detected() =>
            ExceedResourceLifeTimeWithoutDisposing(forceStackTraceCollection: false)
                .Should()
                .NotBeNull();

        [Test] public void And_stack_trace_collection_is_enabled_exception_messages_contains_name_of_allocating_class_and_method_and_managed_type() =>
            ExceedResourceLifeTimeWithoutDisposing(forceStackTraceCollection: true)
                .Message
                .Should()
                .NotBeNullOrEmpty()
                .And
                .Contain(nameof(When_max_lifetime_is_exceeded_without_dispose_being_called))
                .And
                .Contain(nameof(And_stack_trace_collection_is_enabled_exception_messages_contains_name_of_allocating_class_and_method_and_managed_type))
                .And
                .Contain(nameof(StrictResource));
    }
}
