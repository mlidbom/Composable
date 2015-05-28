using Castle.MicroKernel.Registration;
using Composable.ServiceBus;
using FluentAssertions;
using JetBrains.Annotations;
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
                //Review:Rebutal:Richie:In the real scenario, NServiceBus will register IHandleRemoteMessages<AMessage> as IHandleMessages<AMessage>, of course we should register IHandleRemoteMessages<AMessage> as well
                Component.For<ARemoteMessageHandler, IHandleMessages<AMessage>,IHandleRemoteMessages<AMessage>>().ImplementedBy<ARemoteMessageHandler>(),
                Component.For<AInProcessMessageHandler, IHandleInProcessMessages<AMessage>>().ImplementedBy<AInProcessMessageHandler>(),
                Component.For<ASpy, IHandleMessages<AMessage>>().ImplementedBy<ASpy>()
                );
        }

        [Test]
        public void Message_should_be_handled_in_all_handlers_supported_by_synchronous_bus()
        {
            //Act
            SynchronousBus.Publish(new AMessage());

            //Assert
            Container.Resolve<AMessageHandler>().ReceivedMessage.Should().Be(true);
            Container.Resolve<AInProcessMessageHandler>().ReceivedMessage.Should().Be(true);
        }

        [Test]
        public void Message_should_not_be_handled_in_remote_handler()
        {
            //Act
            SynchronousBus.Publish(new AMessage());

            //Assert
            Container.Resolve<ARemoteMessageHandler>().ReceivedMessage.Should().Be(false);
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

        [UsedImplicitly]
        public class ASpy : AMessageHandler, ISynchronousBusMessageSpy { }

        [UsedImplicitly]
        public class AInProcessMessageHandler : IHandleInProcessMessages<AMessage>
        {
            public bool ReceivedMessage;

            public void Handle(AMessage message)
            {
                ReceivedMessage = true;
            }
        }

        [UsedImplicitly]
        public class ARemoteMessageHandler : IHandleRemoteMessages<AMessage>
        {
            public bool ReceivedMessage;

            public void Handle(AMessage message)
            {
                ReceivedMessage = true;
            }
        } 
    }
}
