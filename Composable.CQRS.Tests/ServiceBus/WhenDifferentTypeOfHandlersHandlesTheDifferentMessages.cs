using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.MicroKernel.Registration;
using Composable.ServiceBus;
using FluentAssertions;
using JetBrains.Annotations;
using NServiceBus;
using NUnit.Framework;

namespace CQRS.Tests.ServiceBus
{
    [TestFixture]
    public class WhenDifferentTypeOfHandlersHandlesTheDifferentMessages:MessageHandlersTestBase

    {
        [SetUp]
        public void RegisterHandlers()
        {
            Container.Register(
                Component.For<MessageHandler,
                IHandleMessages<AMessage>,
                IHandleInProcessMessages<InProcessMessage>,
                IHandleMessages<RemoteMessage>
                >().ImplementedBy<MessageHandler>()
                );
        }

        [Test]
        public void When_publishing_a_message_should_be_handled_by_IHandleMessages_handler()
        {
            //Act
            SynchronousBus.Publish(new AMessage());

            //Assert
            var handler = Container.Resolve<MessageHandler>();
            handler.ReceivedAMessage.Should().Be(true);
        }  
        
        [Test]
        public void When_publishing_in_process_message_should_be_handled_by_IHandleInProcessMessages_handler()
        {
            //Act
            SynchronousBus.Publish(new InProcessMessage());

            //Assert
            var handler = Container.Resolve<MessageHandler>();
            handler.ReceivedInProcessMessage.Should().Be(true);
        }

       
        [Test]
        public void When_publishing_remote_message_should_be_handled_by_IHandleRemoteMessages_handler()
        {
            //Act
            SynchronousBus.Publish(new RemoteMessage());

            //Assert
            var handler = Container.Resolve<MessageHandler>();
            handler.ReceivedRemoteMessage.Should().Be(false);
        }

        public class AMessage : IMessage { }
        public class InProcessMessage :IMessage{}
        public class RemoteMessage : IMessage{}

        [UsedImplicitly]
        public class MessageHandler :
            IHandleMessages<AMessage>,
            IHandleInProcessMessages<InProcessMessage>,
            IHandleRemoteMessages<RemoteMessage>
        {
            public bool ReceivedAMessage = false;
            public bool ReceivedInProcessMessage = false;
            public bool ReceivedRemoteMessage = false;

            public void Handle(AMessage message)
            {
                ReceivedAMessage = true;
            }

            public void Handle(InProcessMessage message)
            {
                ReceivedInProcessMessage = true;
            }

            public void Handle(RemoteMessage message)
            {
                ReceivedRemoteMessage = true;
            }

        }
    }
}
