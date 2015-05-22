using Castle.MicroKernel.Registration;
using Composable.ServiceBus;
using FluentAssertions;
using NServiceBus;
using NUnit.Framework;

namespace CQRS.Tests.ServiceBus
{
    [TestFixture]
    public class WhenDifferentTypesOfHandlersHandlesTheSameMessage : MessageHandlersTestBase
    {
        [SetUp]
        public void RegisterHandlers()
        {
            Container.Register(
                Component.For<AMessageHandler, IHandleMessages<AMessage>>().ImplementedBy<AMessageHandler>(),
                Component.For<ARemoteMessageHandler, IHandleMessages<AMessage>>().ImplementedBy<ARemoteMessageHandler>(),
                Component.For<AInProcessMessageHandler, IHandleInProcessMessages<AMessage>>().ImplementedBy<AInProcessMessageHandler>(),
                Component.For<ASpy, IHandleMessages<AMessage>>().ImplementedBy<ASpy>()
                );
        }

        [Test]
        public void Message_should_be_handled_in_all_handlers_supported_by_synchronous_bus()
        {
            SynchronousBus.Publish(new AMessage());

            //Assert
            Container.Resolve<AMessageHandler>().ReceivedMessage.Should().Be(true);
            Container.Resolve<AInProcessMessageHandler>().ReceivedMessage.Should().Be(true);
        }

        [Test]
        public void Message_should_not_be_handled_in_remote_handler()
        {
            SynchronousBus.Publish(new AMessage());

            Container.Resolve<ARemoteMessageHandler>().ReceivedMessage.Should().Be(false);
        }
    }
}
