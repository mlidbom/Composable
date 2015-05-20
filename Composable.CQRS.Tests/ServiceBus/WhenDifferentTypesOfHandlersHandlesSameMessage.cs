using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.ServiceBus;
using Composable.SystemExtensions.Threading;
using FluentAssertions;
using NServiceBus;
using NSpec;
using NUnit.Framework;

namespace CQRS.Tests.ServiceBus
{
    [TestFixture]
    public class WhenDifferentTypesOfHandlersHandlesSameMessage
    {
        private IWindsorContainer _container;
        private SynchronousBus _bus;

        [SetUp]
        public void SetUpContainerAndBeginScope()
        {
            _container = new WindsorContainer();
            _container.Register(
                Component.For<ISingleContextUseGuard>().ImplementedBy<SingleThreadUseGuard>(),
                Component.For<SynchronousBus>(),
                Component.For<IWindsorContainer>().Instance(_container),
                Component.For<AMessageHandler, IHandleMessages<AMessage>>().ImplementedBy<AMessageHandler>(),
                Component.For<ARemoteMessageHandler, IHandleRemoteMessages<AMessage>>().ImplementedBy<ARemoteMessageHandler>(),
                Component.For<AInProcessMessageHandler, IHandleInProcessMessages<AMessage>>().ImplementedBy<AInProcessMessageHandler>(),
                Component.For<ASpy, IHandleMessages<AMessage>>().ImplementedBy<ASpy>()
                );


            _bus = _container.Resolve<SynchronousBus>();
        }

        [Test]
        public void Message_should_be_handled_in_multiple_handlers()
        {
            var message = new AMessage();

            _bus.Publish(message);

            //Assert
            _container.Resolve<AMessageHandler>().ReceivedMessage.Should().Be(true);
            _container.Resolve<AInProcessMessageHandler>().ReceivedMessage.Should().Be(true);
        }

        [Test]
        public void Message_should_not_be_handled_in_remote_handler()
        {
            var message = new AMessage();

            _bus.Publish(message);

            _container.Resolve<ARemoteMessageHandler>().ReceivedMessage.Should().Be(false);
        }
    }

    public class AMessage : IMessage { }

    public class AMessageHandler : IHandleMessages<AMessage>
    {
        public bool ReceivedMessage;

        public void Handle(AMessage message)
        {
            ReceivedMessage = true;
        }
    }

    public class ASpy : AMessageHandler, ISynchronousBusMessageSpy { }

    public class AInProcessMessageHandler : IHandleInProcessMessages<AMessage>
    {
        public bool ReceivedMessage;

        public void Handle(AMessage message)
        {
            ReceivedMessage = true;
        }
    }

    public class ARemoteMessageHandler : IHandleRemoteMessages<AMessage>
    {
        public bool ReceivedMessage;

        public void Handle(AMessage message)
        {
            ReceivedMessage = true;
        }
    }
}
