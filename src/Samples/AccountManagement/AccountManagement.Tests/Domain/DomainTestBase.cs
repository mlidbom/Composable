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
        protected EventSpy EventSpy;

        protected ITestingEndpointHost Host;
        protected IEndpoint ClientEndpoint;

        [SetUp] public void SetupContainerAndBeginScope()
        {
            EventSpy = new EventSpy();
            Host = EndpointHost.Testing.CreateHost(DependencyInjectionContainer.Create);
            ClientEndpoint = Host.ClientEndpoint;

            var domainEndpoint = AccountManagementServerDomainBootstrapper.RegisterWith(Host);
            domainEndpoint.ServiceLocator.Resolve<IMessageHandlerRegistrar>()
                           .ForEvent<ITransactionalExactlyOnceDeliveryEvent>(EventSpy.Receive);
        }

        [TearDown] public void DisposeScopeAndContainer() => Host.Dispose();
    }
}
