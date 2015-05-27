using Castle.MicroKernel.Registration;
using Composable.ServiceBus;
using FluentAssertions;
using NServiceBus;
using NUnit.Framework;

namespace CQRS.Tests.ServiceBus
{
    [TestFixture]
    public class WhenReplayAEvent : MessageHandlersTestBase
    {
        [SetUp]
        public void RegisterHandler()
        {
            Container.Register(
                Component.For<AHandleReplayedEventsHandler, IHandleReplayedEvents<AMessage>>().ImplementedBy<AHandleReplayedEventsHandler>(),
                Component.For<AHandleReplayedAndPublishedEventsHandler, IHandleReplayedEvents<AMessage>>()
                    .ImplementedBy<AHandleReplayedAndPublishedEventsHandler>(),
                    Component.For<ASpy, IHandleMessages<AMessage>>().ImplementedBy<ASpy>()
                );

            Container.Register(
                Component.For<MessageHandler,
                IHandleReplayedEvents<ReplayEventBase>,
                IHandleReplayedEvents<HandleAndReplayEventBase>
                >().ImplementedBy<MessageHandler>()
                );
        }

        [Test]
        [Ignore]
        public void Event_should_be_replayed_in_all_handlers_supported_by_synchronous_bus()
        {
//            SynchronousBus.Replay(new AMessage());
//
//            //Assert
//            Container.Resolve<AHandleReplayedEventsHandler>().ReceivedMessage.Should().Be(true);
//            Container.Resolve<AHandleReplayedAndPublishedEventsHandler>().ReceivedMessage.Should().Be(true);
        }

        [Test]
        [Ignore]
        public void When_replay_a_replayed_event()
        {
//            SynchronousBus.Replay(new ReplayEvent());
//
//            //Assert
//            Container.Resolve<MessageHandler>().ReceivedAMessage.Should().Be(false);
//            Container.Resolve<MessageHandler>().ReceivedInProcessMessage.Should().Be(false);
//            Container.Resolve<MessageHandler>().ReceivedRemoteMessage.Should().Be(false);
//            Container.Resolve<MessageHandler>().ReceivedReplayEvent.Should().Be(true);
//            Container.Resolve<MessageHandler>().ReceivedHandleAndReplayEvent.Should().Be(false);
        }

        [Test]
        [Ignore]
        public void When_replay_a_handle_and_replayed_event()
        {
//            SynchronousBus.Replay(new HandleAndReplayEvent());
//
//            //Assert
//            Container.Resolve<MessageHandler>().ReceivedAMessage.Should().Be(false);
//            Container.Resolve<MessageHandler>().ReceivedInProcessMessage.Should().Be(false);
//            Container.Resolve<MessageHandler>().ReceivedRemoteMessage.Should().Be(false);
//            Container.Resolve<MessageHandler>().ReceivedReplayEvent.Should().Be(false);
//            Container.Resolve<MessageHandler>().ReceivedHandleAndReplayEvent.Should().Be(true);
        }
    }
}
