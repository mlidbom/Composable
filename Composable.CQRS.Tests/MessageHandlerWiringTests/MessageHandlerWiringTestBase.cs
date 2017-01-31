using System;
using System.Collections.Generic;
using Castle.MicroKernel.Lifestyle;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.CQRS.EventSourcing;
using Composable.ServiceBus;
using Composable.Windsor.Testing;
using CQRS.Tests.ServiceBus;
using NUnit.Framework;

namespace CQRS.Tests.MessageHandlerWiringTests
{
    [TestFixture]
    public class MessageHandlerWiringTestBase
    {
        protected WindsorContainer Container;
        IDisposable _scope;

        public SynchronousBus SynchronousBus { get { return Container.Resolve<SynchronousBus>(); } }
        public EventsReplayer EventsReplayer { get { return Container.Resolve<EventsReplayer>(); } }
        public ReplayedAndPublishedEventsHandler ReplayedAndPublishedEventsHandler { get { return Container.Resolve<ReplayedAndPublishedEventsHandler>(); } }
        public ReplayedEventsHandler ReplayedEventsHandler { get { return Container.Resolve<ReplayedEventsHandler>(); } }
        public InProcessEventsHandler InProcessEventsHandler { get { return Container.Resolve<InProcessEventsHandler>(); } }
        public RemoteOnlyEventsHandler RemoteOnlyEventsHandler { get { return Container.Resolve<RemoteOnlyEventsHandler>(); } }
        public NormalNServiceBusHandler NormalNServiceBusHandler { get { return Container.Resolve<NormalNServiceBusHandler>(); } }
        public SupporttedAllHandlerInterfacesMessageHandler SupporttedAllHandlerInterfacesMessageHandler { get { return Container.Resolve<SupporttedAllHandlerInterfacesMessageHandler>(); } }


        [SetUp]
        public void SetUpContainerAndBeginScope()
        {
            Container = new WindsorContainer();
            Container.ConfigureWiringForTestsCallBeforeAllOtherWiring();
            Container.Register(
                Component.For<SynchronousBus>(),
                Component.For<EventsReplayer>(),
                Component.For<IWindsorContainer>().Instance(Container));
            new MessageHandlersRegister().RegisterMessageHandlersForTestingFromAssemblyContaining<MessageHandlersTestBase>(Container);
            Container.ConfigureWiringForTestsCallAfterAllOtherWiring();
            _scope = Container.BeginScope();
        }

        [TearDown]
        public void TearDown()
        {
            _scope.Dispose();
        }
    }

    public class AccountCreatedEvent : IEvent
    {
    }

    public class ReplayedAndPublishedEventsHandler : IHandleReplayedAndPublishedEvents<AccountCreatedEvent>
    {
        public List<IEvent> HandledMessages = new List<IEvent>();

        public void Handle(AccountCreatedEvent message)
        {
            HandledMessages.Add(message);
        }
    }

    public class ReplayedEventsHandler : IHandleReplayedEvents<AccountCreatedEvent>
    {
        public List<IEvent> HandledMessages = new List<IEvent>();

        public void Handle(AccountCreatedEvent message)
        {
            HandledMessages.Add(message);
        }
    }

    public class InProcessEventsHandler : IHandleInProcessMessages<AccountCreatedEvent>
    {
        public List<AccountCreatedEvent> HandledMessages = new List<AccountCreatedEvent>();

        public void Handle(AccountCreatedEvent message)
        {
            HandledMessages.Add(message);
        }
    }

    public class RemoteOnlyEventsHandler : IHandleRemoteMessages<AccountCreatedEvent>
    {
        public List<AccountCreatedEvent> HandledMessages = new List<AccountCreatedEvent>();

        public void Handle(AccountCreatedEvent message)
        {
            HandledMessages.Add(message);
        }
    }

    public class NormalNServiceBusHandler : IHandleMessages<AccountCreatedEvent>
    {
        public List<AccountCreatedEvent> HandledMessages = new List<AccountCreatedEvent>();

        public void Handle(AccountCreatedEvent message)
        {
            HandledMessages.Add(message);
        }
    }

    public class SupporttedAllHandlerInterfacesMessageHandler :
        IHandleReplayedEvents<AccountCreatedEvent>,
        IHandleInProcessMessages<AccountCreatedEvent>,
        IHandleRemoteMessages<AccountCreatedEvent>
    {
        public List<AccountCreatedEvent> HandledReplayedMessages = new List<AccountCreatedEvent>();
        public List<AccountCreatedEvent> HandledInProcessMessages = new List<AccountCreatedEvent>();
        public List<AccountCreatedEvent> HandledNormalNServiceBusMessages = new List<AccountCreatedEvent>();

        void IHandleReplayedEvents<AccountCreatedEvent>.Handle(AccountCreatedEvent message)
        {
            HandledReplayedMessages.Add(message);
        }

        void IHandleInProcessMessages<AccountCreatedEvent>.Handle(AccountCreatedEvent message)
        {
            HandledInProcessMessages.Add(message);
        }

        void IHandleMessages<AccountCreatedEvent>.Handle(AccountCreatedEvent message)
        {
            HandledNormalNServiceBusMessages.Add(message);
        }
    }
}
