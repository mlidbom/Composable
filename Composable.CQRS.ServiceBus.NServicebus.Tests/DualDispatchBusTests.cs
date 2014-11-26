using System;
using System.Collections.Generic;
using Castle.MicroKernel.Lifestyle;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.CQRS.ServiceBus.NServiceBus;
using Composable.CQRS.Windsor;
using Composable.ServiceBus;
using Composable.SystemExtensions.Threading;
using FluentAssertions;
using Moq;
using NServiceBus;
using NServiceBus.Unicast;
using NUnit.Framework;

namespace Composable.CQRS.ServiceBus.NServicebus.Tests
{
    [TestFixture]
    public class DualDispatchBusTests
    {
        [Test]
        public void WhenSendingMessageThatHasNoWiredHandlerInContainerMessageIsSentOnNServiceBusBus()
        {
            var container = SetupContainerWithBusesRegisteredButNoHandlersAndAMockForIBus();

            var busMock = container.Resolve<Mock<IBus>>();

            var message = new Message();
            busMock.Setup(bus => bus.Send(message)).Returns(new Callback(null)).Verifiable();
            busMock.Setup(bus => bus.OutgoingHeaders).Returns(new Dictionary<string, string>());

            container.Register(Component.For<IWindsorContainer>().Instance(container));

            using (container.BeginScope())
            {
                container.Resolve<IServiceBus>().Send(message);
                busMock.Verify();
            }
        }


        [Test]
        public void WhenSendingMessageThatHasAWiredHandlerInContainerMessageIsSentOnSynchronousBus()
        {
            var container = SetupContainerWithBusesRegisteredButNoHandlersAndAMockForIBus();
            container.Register(Component.For<AMessageHandler, IHandleMessages<Message>>().ImplementedBy<AMessageHandler>());

            container.Register(Component.For<IWindsorContainer>().Instance(container));

            using (container.BeginScope())
            {
                container.Resolve<IServiceBus>().Send(new Message());
                container.Resolve<AMessageHandler>().Handled.Should().Be(true);
            }
        }

        [Test]
        public void WhenReplyingWithinAHandlerCalledByTheSynchronousBusTheReplyGoesToTheSynchronousBus()
        {
            var container = WireUpDualDispatchBusWithAllLocalMessageHandlersRegisteredAndAStrictMockForTheRealIBus();

            using (container.BeginScope())
            {
                container.Resolve<IServiceBus>().Send(new ReplyCommand());
                container.Resolve<ACommandReplyHandler>().ReplyReceived.Should().Be(true);
            }
        }

        [Test]
        public void WhenReplyingWithinAHandlerCalledByTheSynchronousBusInFashionCausingReentrancyTheReplyGoesToTheSynchronousBus()
        {
            var container = WireUpDualDispatchBusWithAllLocalMessageHandlersRegisteredAndAStrictMockForTheRealIBus();

            using (container.BeginScope())
            {
                container.Resolve<IServiceBus>().Send(new SendAMessageAndThenReplyCommand());
                container.Resolve<ACommandReplyHandler>().ReplyReceived.Should().Be(true);
            }
        }

        [Test]
        public void WhenSendingMessageThatWillCauseTheExpectionShouldRecevieOriginalException()
        {
            var container = SetupContainerWithBusesRegisteredButNoHandlersAndAMockForIBus();
            container.Register(Component.For<MessageWillCauseExceptionHandler, IHandleMessages<MessageWillCauseException>>().ImplementedBy<MessageWillCauseExceptionHandler>());

            container.Register(Component.For<IWindsorContainer>().Instance(container));

            using (container.BeginScope())
            {
                Assert.Throws<ExpectException>(() => container.Resolve<IServiceBus>().Send(new MessageWillCauseException()));
            }
        }

        public class ACommandReplyHandler : IHandleMessages<ReplyMessage>
        {
            public bool ReplyReceived { get; set; }

            public void Handle(ReplyMessage message)
            {
                ReplyReceived = true;
            }
        }

        public class SendAMessageAndThenReplyCommandCommandHandler : IHandleMessages<SendAMessageAndThenReplyCommand>
        {
            private readonly IServiceBus _bus;

            public SendAMessageAndThenReplyCommandCommandHandler(IServiceBus bus)
            {
                _bus = bus;
            }

            public void Handle(SendAMessageAndThenReplyCommand message)
            {
                _bus.Send(new Message());
                _bus.Reply(new ReplyCommand());
            }
        }

        public class ACommandHandler : IHandleMessages<ReplyCommand>
        {
            private readonly IServiceBus _bus;

            public ACommandHandler(IServiceBus bus)
            {
                _bus = bus;
            }

            public void Handle(ReplyCommand message)
            {
                _bus.Reply(new ReplyMessage());
            }
        }

        public class AMessageHandler : IHandleMessages<Message>
        {
            public void Handle(Message message)
            {
                Handled = true;
            }

            public bool Handled { get; set; }
        }

        public class MessageWillCauseExceptionHandler : IHandleMessages<MessageWillCauseException>
        {
            public void Handle(MessageWillCauseException message)
            {
                throw new ExpectException();
            }
        }
        public class ExpectException : Exception { }

        public class Message : IMessage { }

        public class ReplyMessage : IMessage { }

        public class ReplyCommand : IMessage { }

        public class SendAMessageAndThenReplyCommand : IMessage { }

        public class MessageWillCauseException : IMessage { }

        private static WindsorContainer SetupContainerWithBusesRegisteredButNoHandlersAndAMockForIBus()
        {
            var container = new WindsorContainer();
            var busMock1 = new Mock<IBus>(MockBehavior.Strict);
            container.Register(
                Component.For<Mock<IBus>>().Instance(busMock1),
                Component.For<IBus>().Instance(busMock1.Object),
                Component.For<ISingleContextUseGuard>().ImplementedBy<SingleThreadUseGuard>(),
                Component.For<SynchronousBus>(),
                Component.For<NServiceBusServiceBus>(),
                Component.For<IServiceBus>().ImplementedBy<DualDispatchBus>()
                );
            return container;
        }

        private static WindsorContainer WireUpDualDispatchBusWithAllLocalMessageHandlersRegisteredAndAStrictMockForTheRealIBus()
        {
            var container = SetupContainerWithBusesRegisteredButNoHandlersAndAMockForIBus();
            container.Register(
                Component.For<IHandleMessages<ReplyCommand>>().ImplementedBy<ACommandHandler>(),
                Component.For<ACommandReplyHandler, IHandleMessages<ReplyMessage>>().ImplementedBy<ACommandReplyHandler>(),
                Component.For<IHandleMessages<SendAMessageAndThenReplyCommand>>().ImplementedBy<SendAMessageAndThenReplyCommandCommandHandler>(),
                Component.For<IHandleMessages<Message>>().ImplementedBy<AMessageHandler>()
                );

            container.Register(Component.For<IWindsorContainer>().Instance(container));
            return container;
        }
    }
}