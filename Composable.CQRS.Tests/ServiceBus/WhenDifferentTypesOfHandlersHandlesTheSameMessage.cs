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
                //Review:mlidbo: If you do not register it as IHandleRemoteMessages, of course that interface member will never be called.
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

            //Review:mlidbo: What are you really trying to test here? Because what you are testing is impossible. AMessage does not inherit RemoteMessage. It is impossible for the runtime to cast it to that type. So it is impossible for that handler to be called by publishing that event.
            Container.Resolve<ARemoteMessageHandler>().ReceivedMessage.Should().Be(false);
        }
    }
}
