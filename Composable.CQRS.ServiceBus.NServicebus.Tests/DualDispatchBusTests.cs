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
using NUnit.Framework;

namespace Composable.CQRS.ServiceBus.NServicebus.Tests
{
    [TestFixture]
    public class DualDispatchBusTests
    {
        [Test]
        public void WhenReplyingWithinAHandlerCalledByTheSynchronousBusTheReplyGoesToTheSynchronousBus()
        {
            var container = WireUpDualDispatchBusWithAllLocalMessageHandlersRegisteredAndAStrictMockForTheRealIBus();

            using(container.BeginScope())
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

        public class Message : IMessage {}

        public class ReplyMessage : IMessage { }

        public class ReplyCommand : IMessage { }

        public class SendAMessageAndThenReplyCommand : IMessage { }

        private static WindsorContainer WireUpDualDispatchBusWithAllLocalMessageHandlersRegisteredAndAStrictMockForTheRealIBus()
        {
            var container = new WindsorContainer();
            container.Register(
                Component.For<IBus>().Instance(new Mock<IBus>(MockBehavior.Strict).Object),
                Component.For<ISingleContextUseGuard>().ImplementedBy<SingleThreadUseGuard>(),
                Component.For<SynchronousBus>(),
                Component.For<NServiceBusServiceBus>(),
                Component.For<IServiceBus>().ImplementedBy<DualDispatchBus>().LifestyleScoped(),
                Component.For<IHandleMessages<ReplyCommand>>().ImplementedBy<ACommandHandler>(),
                Component.For<ACommandReplyHandler, IHandleMessages<ReplyMessage>>().ImplementedBy<ACommandReplyHandler>(),
                Component.For<IHandleMessages<SendAMessageAndThenReplyCommand>>().ImplementedBy<SendAMessageAndThenReplyCommandCommandHandler>()
                );

            container.Register(Component.For<IWindsorContainer>().Instance(container));
            return container;
        }
    }   
}