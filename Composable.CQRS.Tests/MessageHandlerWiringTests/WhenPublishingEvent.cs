using System.Linq;
using CQRS.Tests.ServiceBus;
using FluentAssertions;
using NUnit.Framework;

namespace CQRS.Tests.MessageHandlerWiringTests
{
    [TestFixture]
    public class WhenPublishingEvent : MessageHandlerWiringTestBase
    {
        private AccountCreatedEvent _event;

        [SetUp]
        public void PublishEvent()
        {
            // Arrange
            _event = new AccountCreatedEvent();

            // Act
            SynchronousBus.Publish(_event);
        }

        [Test]
        public void Message_should_be_handled_in_all_handlers_supported_by_synchronous_bus()
        {
            // Assert
            NormalNServiceBusHandler.HandledMessages.Single().Should().Be(_event);
            InProcessEventsHandler.HandledMessages.Single().Should().Be(_event);
            ReplayedAndPublishedEventsHandler.HandledMessages.Single().Should().Be(_event);
        }

        [Test]
        public void Message_should_not_be_handled_in_RemoteOnlyEventsHandler_and_ReplayedEventsHandler()
        {
            ReplayedEventsHandler.HandledMessages.Should().BeEmpty();
            RemoteOnlyEventsHandler.HandledMessages.Should().BeEmpty();
        }

        [Test]
        public void Message_should_can_ben_handled_by_seperate_Handle_methods()
        {
            //TODO: this is a bug, but it is a uncommon case.
            //            SupporttedAllHandlerInterfacesMessageHandler.HandledInProcessMessages.Single().Should().Be(evt);
            //            SupporttedAllHandlerInterfacesMessageHandler.HandledNormalNServiceBusMessages.Single().Should().Be(evt);
            SupporttedAllHandlerInterfacesMessageHandler.HandledReplayedMessages.Should().BeEmpty();
        }
    }
}
