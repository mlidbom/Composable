using AccountManagement.TestHelpers;
using Composable.DependencyInjection;
using Composable.System;
using NUnit.Framework;

namespace AccountManagement.Domain.Tests
{
  using Composable.Messaging.Buses;

  [TestFixture] public abstract class DomainTestBase
    {
        protected IServiceLocator Container { get; private set; }
        protected IMessageSpy MessageSpy => Container.Lease<IMessageSpy>().Instance;

        StrictAggregateDisposable _managedResources;

        [SetUp] public void SetupContainerAndBeginScope()
        {
            Container = DomainTestWiringHelper.SetupContainerForTesting();

            _managedResources = StrictAggregateDisposable.Create(Container.BeginScope(), Container);
        }

        [TearDown] public void DisposeScopeAndContainer() { _managedResources.Dispose(); }
    }
}
