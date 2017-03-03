using System;
using Composable.System;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.StrictlyManagedResource
{
    [TestFixture]
    public class When_no_gc_root_exists_for_object_and_garbage_collector_runs_and_instance_is_disposed
    {
        StrictlyManagedResourceWasFinalizedException _leakedResource;

        class StrictResource : IStrictlyManagedResource
        {
            public void Dispose() { }
        }

        [SetUp]
        public void SetupTask()
        {
            _leakedResource = null;
            StrictlyManagedResource<StrictResource>.ThrowCreatedException = resource => _leakedResource = resource;

            // ReSharper disable once ObjectCreationAsStatement
            ((Action)(() => new StrictlyManagedResource<StrictResource>().Dispose())).Invoke();

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        [Test]
        public void No_leak_is_detected() => _leakedResource.Should()
                                                            .BeNull();
    }
}