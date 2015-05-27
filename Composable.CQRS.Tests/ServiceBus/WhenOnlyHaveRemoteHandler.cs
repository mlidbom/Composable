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
    public class WhenOnlyHaveRemoteHandler:MessageHandlersTestBase
    {
        [SetUp]
        public void RegisterHandler()
        {
            //Review:mlidbo: If you want to test that IHandleRemoteMessages<AMessage> is not called, then you have to register an implementation of IHandleRemoteMessages<AMessage>
            //Review:mlidbo: I do not think this test tests what you want it to.
            Container.Register(Component.For<ARemoteMessageHandler,IHandleMessages<AMessage>>().ImplementedBy<ARemoteMessageHandler>());
        }

        [Test]
        public void Should_no_resolver_can_find_handler()
        {
            SynchronousBus.Publish(new AMessage());

            //Review:mlidbo: What are you really trying to test here? Because what you are testing is impossible. AMessage does not inherit RemoteMessage. It is impossible for the runtime to cast it to that type. So it is impossible for that handler to be called by publishing that event.
            Container.Resolve<ARemoteMessageHandler>().ReceivedMessage.Should().Be(false);

        }
    }
}
