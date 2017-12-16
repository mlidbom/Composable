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
        ITestingEndpointHost _host;
        IEndpoint _domainEndpoint;
        protected IServiceBus ClientBus => _host.ClientBus;

        [SetUp] public void SetupContainerAndBeginScope()
        {
            _host = EndpointHost.Testing.CreateHost(DependencyInjectionContainer.Create);
            _domainEndpoint = AccountManagementServerDomainBootstrapper.RegisterWith(_host);

            ServiceLocator = _domainEndpoint.ServiceLocator;

            _managedResources = StrictAggregateDisposable.Create(ServiceLocator.BeginScope(), _host);
        }

        [TearDown] public void DisposeScopeAndContainer() { _managedResources.Dispose(); }
    }
}
