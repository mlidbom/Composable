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
        //Review:mlidbo: Try to name the test after what you expect the result to be. This seems more like the name of a test class
        public void When_publishing_a_message()
        {
            SynchronousBus.Publish(new AMessage());

            var handler = Container.Resolve<MessageHandler>();

            handler.ReceivedAMessage.Should().Be(true);
            //Review:mlidbo: What are you really trying to test here? Because what you are testing is impossible. AMessage does not inherit InProcessMessage or RemoteMessageBase. It is impossible for the runtime to cast it to those types. So it is impossible for these handlers to be called by publishing that event.
            handler.ReceivedInProcessMessage.Should().Be(false);
            handler.ReceivedRemoteMessage.Should().Be(false);
        }  
        
        [Test]
        //Review:mlidbo: Try to name the test after what you expect the result to be. This seems more like the name of a test class
        public void When_publishing_in_process_message()
        {
            SynchronousBus.Publish(new InProcessMessage());

            var handler = Container.Resolve<MessageHandler>();
            
            handler.ReceivedInProcessMessage.Should().Be(true);
            //Review:mlidbo: What are you really trying to test here? Because what you are testing is impossible. InProcessMessage does not inherit AMessage or RemoteMessageBase. It is impossible for the runtime to cast it to those types. So it is impossible for these handlers to be called by publishing that event.
            handler.ReceivedAMessage.Should().Be(false);
            handler.ReceivedRemoteMessage.Should().Be(false);
        }

       
        [Test]
        //Review:mlidbo: Try to name the test after what you expect the result to be. This seems more like the name of a test class
        public void When_publishing_remote_message()
        {
            SynchronousBus.Publish(new RemoteMessage());

            var handler = Container.Resolve<MessageHandler>();
            
            handler.ReceivedRemoteMessage.Should().Be(false);
            //Review:mlidbo: What are you really trying to test here? Because what you are testing is impossible. RemoteMessage does not inherit AMessage or InProcessMessage. It is impossible for the runtime to cast it to those types. So it is impossible for these handlers to be called by publishing that event.
            handler.ReceivedAMessage.Should().Be(false);
            handler.ReceivedInProcessMessage.Should().Be(false);
        }


    }
}
