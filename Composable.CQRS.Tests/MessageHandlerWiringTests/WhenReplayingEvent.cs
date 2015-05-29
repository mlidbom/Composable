using System.Linq;
using CQRS.Tests.ServiceBus;
using FluentAssertions;
using NUnit.Framework;

namespace CQRS.Tests.MessageHandlerWiringTests
{
    [TestFixture]
    public class WhenReplayingEvent : MessageHandlerWiringTestBase
    {
        private AccountCreatedEvent _event;

        [SetUp]
        public void ReplayEvent()
        {
            // Arrange
            _event = new AccountCreatedEvent();

            // Act
            EventsReplayer.Replay(_event);
        }

        [Test]
        public void Message_should_be_handled_in_all_handlers_that_implements_IHandleReplayedEvent()
        {
            // Assert
            ReplayedAndPublishedEventsHandler.HandledMessages.Single().Should().Be(_event);
            ReplayedEventsHandler.HandledMessages.Single().Should().Be(_event);
        }

        [Test]
        public void Message_should_not_be_handled_in_all_handlers_that_are_not_implements_IHandleReplayedEvent()
        {
            RemoteOnlyEventsHandler.HandledMessages.Should().BeEmpty();
            NormalNServiceBusHandler.HandledMessages.Should().BeEmpty();
            InProcessEventsHandler.HandledMessages.Should().BeEmpty();
        }
    }
}
