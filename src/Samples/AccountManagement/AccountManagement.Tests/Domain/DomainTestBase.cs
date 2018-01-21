using System.Collections.Generic;
using System.Linq;
using Composable.DependencyInjection;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Composable.System;
using NUnit.Framework;

namespace AccountManagement.Tests.Domain
{
    public class EventSpy
    {
        public IEnumerable<ITransactionalExactlyOnceDeliveryEvent> DispatchedMessages => _events.ToList();
        public void Receive(ITransactionalExactlyOnceDeliveryEvent @event) { _events.Add(@event); }
        List<ITransactionalExactlyOnceDeliveryEvent> _events = new List<ITransactionalExactlyOnceDeliveryEvent>();
    }

    [TestFixture] public abstract class DomainTestBase
    {
        protected IServiceLocator ServiceLocator { get; private set; }
        protected EventSpy EventSpy;

        StrictAggregateDisposable _managedResources;
        protected ITestingEndpointHost Host;
        IEndpoint _domainEndpoint;
        protected IServiceBusSession ServerBusSession => _domainEndpoint.ServiceLocator.Resolve<IServiceBusSession>();

        [SetUp] public void SetupContainerAndBeginScope()
        {
            EventSpy = new EventSpy();
            Host = EndpointHost.Testing.CreateHost(DependencyInjectionContainer.Create);
            _domainEndpoint = AccountManagementServerDomainBootstrapper.RegisterWith(Host);
            _domainEndpoint.ServiceLocator.Resolve<IMessageHandlerRegistrar>()
                           .ForEvent<ITransactionalExactlyOnceDeliveryEvent>(EventSpy.Receive);

            ServiceLocator = _domainEndpoint.ServiceLocator;

            _managedResources = StrictAggregateDisposable.Create(ServiceLocator.BeginScope(), Host);
        }

        [TearDown] public void DisposeScopeAndContainer() { _managedResources.Dispose(); }
    }
}
