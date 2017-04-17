using Composable.DependencyInjection;
using Composable.Messaging.Buses;
using Composable.System;
using NUnit.Framework;

namespace AccountManagement.Tests.Domain
{
    [TestFixture] public abstract class DomainTestBase
    {
        protected IServiceLocator ServiceLocator { get; private set; }
        protected IMessageSpy MessageSpy => ServiceLocator.Lease<IMessageSpy>().Instance;

        StrictAggregateDisposable _managedResources;

        [SetUp] public void SetupContainerAndBeginScope()
        {
            ServiceLocator = DomainTestWiringHelper.CreateServiceLocator();

            _managedResources = StrictAggregateDisposable.Create(ServiceLocator.BeginScope(), ServiceLocator);
        }

        [TearDown] public void DisposeScopeAndContainer() { _managedResources.Dispose(); }
    }
}
