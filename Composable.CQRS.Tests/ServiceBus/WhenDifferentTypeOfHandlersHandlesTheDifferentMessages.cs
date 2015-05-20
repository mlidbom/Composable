using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.MicroKernel.Registration;
using Composable.ServiceBus;
using FluentAssertions;
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
                IHandleMessages<RemoteMessageBase>
                >().ImplementedBy<MessageHandler>()
                );
        }

        [Test]
        public void When_publishing_a_message()
        {
            SynchronousBus.Publish(new AMessage());

            var handler = Container.Resolve<MessageHandler>();

            handler.ReceiveAMessage.Should().Be(true);
            handler.ReceiveInProcessMessage.Should().Be(false);
            handler.ReceiveRemoteMessage.Should().Be(false);
        }  
        
        [Test]
        public void When_publishing_in_process_message()
        {
            SynchronousBus.Publish(new InProcessMessage());

            var handler = Container.Resolve<MessageHandler>();

            handler.ReceiveAMessage.Should().Be(false);
            handler.ReceiveInProcessMessage.Should().Be(true);
            handler.ReceiveRemoteMessage.Should().Be(false);
        }

       
        [Test]
        public void When_publishing_remote_message()
        {
            SynchronousBus.Publish(new RemoteMessage());

            var handler = Container.Resolve<MessageHandler>();

            handler.ReceiveAMessage.Should().Be(false);
            handler.ReceiveInProcessMessage.Should().Be(false);
            handler.ReceiveRemoteMessage.Should().Be(false);
        }


    }
}
