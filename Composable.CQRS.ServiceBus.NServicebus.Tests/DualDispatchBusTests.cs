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
            var container = new WindsorContainer();
            container.Register(                
                Component.For<IBus>().Instance(new Mock<IBus>(MockBehavior.Strict).Object),
                Component.For<ISingleContextUseGuard>().ImplementedBy<SingleThreadUseGuard>(),
                Component.For<SynchronousBus>(),
                Component.For<NServiceBusServiceBus>(),
                Component.For<IServiceBus>().ImplementedBy<DualDispatchBus>().LifestyleScoped(),
                Component.For<IHandleMessages<ACommand>>().ImplementedBy<ACommandHandler>(),
                Component.For<ACommandReplyHandler,IHandleMessages<ACommandReply>>().ImplementedBy<ACommandReplyHandler>()
                );

            container.Register(Component.For<IWindsorContainer>().Instance(container));

            using(container.BeginScope())
            {
                container.Resolve<IServiceBus>().Send(new ACommand());
                container.Resolve<ACommandReplyHandler>().ReplyReceived.Should().Be(true);

            }
        }

        public class ACommandReplyHandler : IHandleMessages<ACommandReply>
        {
            public void Handle(ACommandReply message)
            {
                ReplyReceived = true;
            }

            public bool ReplyReceived { get; set; }
        }

        public class ACommandHandler : IHandleMessages<ACommand>
        {
            private readonly IServiceBus _bus;

            public ACommandHandler(IServiceBus bus)
            {
                _bus = bus;
            }

            public void Handle(ACommand message)
            {
                _bus.Reply(new ACommandReply());
            }
        }

        public class ACommandReply : IMessage { }

        public class ACommand : IMessage  { }
    }   
}