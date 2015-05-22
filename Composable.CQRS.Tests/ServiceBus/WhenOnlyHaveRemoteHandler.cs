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
            Container.Register(Component.For<ARemoteMessageHandler,IHandleMessages<AMessage>>().ImplementedBy<ARemoteMessageHandler>());
        }

        [Test]
        public void Should_no_resolver_can_find_handler()
        {
            SynchronousBus.Publish(new AMessage());

            Container.Resolve<ARemoteMessageHandler>().ReceivedMessage.Should().Be(false);

        }
    }
}
