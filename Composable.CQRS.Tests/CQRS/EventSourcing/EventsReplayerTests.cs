using Composable.CQRS.EventSourcing;
using Composable.ServiceBus;
using NUnit.Framework;

namespace CQRS.Tests.CQRS.EventSourcing
{
    public class EventsReplayerTests
    {
        public class AccountCreatedEvent : IEvent
        {
        }

      

        [Test]
        public void When_replaying_events_that_should_only_be_handled_by_IHandleReplayedMessage()
        {

        }
    }
}
