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
        readonly List<ITransactionalExactlyOnceDeliveryEvent> _events = new List<ITransactionalExactlyOnceDeliveryEvent>();
    }

    [TestFixture] public abstract class DomainTestBase
    {
        protected IServiceLocator ServiceLocator { get; private set; }
        protected EventSpy EventSpy;

        protected ITestingEndpointHost Host;
        protected IEndpoint DomainEndpoint;

        [SetUp] public void SetupContainerAndBeginScope()
        {
            EventSpy = new EventSpy();
            Host = EndpointHost.Testing.CreateHost(DependencyInjectionContainer.Create);
            DomainEndpoint = AccountManagementServerDomainBootstrapper.RegisterWith(Host);
            DomainEndpoint.ServiceLocator.Resolve<IMessageHandlerRegistrar>()
                           .ForEvent<ITransactionalExactlyOnceDeliveryEvent>(EventSpy.Receive);

            ServiceLocator = DomainEndpoint.ServiceLocator;

        }

        [TearDown] public void DisposeScopeAndContainer() => Host.Dispose();
    }
}
