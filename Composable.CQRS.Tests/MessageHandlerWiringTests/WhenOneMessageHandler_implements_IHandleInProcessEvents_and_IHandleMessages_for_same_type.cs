using System.Collections.Generic;
using System.Linq;
using Composable.ServiceBus;
using FluentAssertions;
using NUnit.Framework;

namespace CQRS.Tests.MessageHandlerWiringTests
{
    public class MessageHandlerForHandleInProcessEventsAndHandleMessages : IHandleInProcessMessages<AccountCreatedEvent>, IHandleMessages<AccountCreatedEvent>
    {
        public List<AccountCreatedEvent> HandledEvents = new List<AccountCreatedEvent>();

        public void Handle(AccountCreatedEvent message)
        {
            HandledEvents.Add(message);
        }
    }

    public class WhenOneMessageHandler_implements_IHandleInProcessEvents_and_IHandleMessages_for_same_message : MessageHandlerWiringTestBase
    {
        [Test]
        public void That_should_be_handled_once()
        {
            var evt = new AccountCreatedEvent();

            SynchronousBus.Publish(evt);

            // Assert
            Container.Resolve<MessageHandlerForHandleInProcessEventsAndHandleMessages>().HandledEvents.Single().Should().Be(evt);
        }
    }
}
