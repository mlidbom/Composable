using AccountManagement.API;
using Composable.DependencyInjection;
using Composable.Messaging;
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
        protected ITestingEndpointHost Host;
        IEndpoint _domainEndpoint;
        protected IServiceBus ClientBus => Host.ClientBus;

        [SetUp] public void SetupContainerAndBeginScope()
        {
            Host = EndpointHost.Testing.CreateHost(DependencyInjectionContainer.Create);
            var test = new EntityByIdQuery<AccountResource>();
            _domainEndpoint = AccountManagementServerDomainBootstrapper.RegisterWith(Host);

            ServiceLocator = _domainEndpoint.ServiceLocator;

            _managedResources = StrictAggregateDisposable.Create(ServiceLocator.BeginScope(), Host);
        }

        [TearDown] public void DisposeScopeAndContainer() { _managedResources.Dispose(); }
    }
}
