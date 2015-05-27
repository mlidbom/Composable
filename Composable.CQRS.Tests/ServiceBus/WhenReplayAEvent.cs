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
    public class WhenReplayAEvent:MessageHandlersTestBase
    {
        [SetUp]
        public void RegisterHandler()
        {
            Container.Register(
                Component.For<AReplayEventsHandler, IReplayEvents<AMessage>>().ImplementedBy<AReplayEventsHandler>(),
                Component.For<AHandleAndReplayEventsHandler, IReplayEvents<AMessage>>()
                    .ImplementedBy<AHandleAndReplayEventsHandler>(),
                    Component.For<ASpy, IHandleMessages<AMessage>>().ImplementedBy<ASpy>()
                );

            Container.Register(
                Component.For<MessageHandler,
                IReplayEvents<ReplayEventBase>,
                IReplayEvents<HandleAndReplayEventBase>
                >().ImplementedBy<MessageHandler>()
                );
        }

        [Test]
        public void Event_should_be_replayed_in_all_handlers_supported_by_synchronous_bus()
        {
            SynchronousBus.Replay(new AMessage());

            //Assert
            Container.Resolve<AReplayEventsHandler>().ReceivedMessage.Should().Be(true);
            Container.Resolve<AHandleAndReplayEventsHandler>().ReceivedMessage.Should().Be(true);
        }

        [Test]
        public void When_replay_a_replay_event()
        {
            SynchronousBus.Replay(new ReplayEvent());

            //Assert
            Container.Resolve<MessageHandler>().ReceiveAMessage.Should().Be(false);
            Container.Resolve<MessageHandler>().ReceiveInProcessMessage.Should().Be(false);
            Container.Resolve<MessageHandler>().ReceiveRemoteMessage.Should().Be(false);
            Container.Resolve<MessageHandler>().ReceiveReplayEvent.Should().Be(true);
            Container.Resolve<MessageHandler>().ReceiveHandleAndReplayEvent.Should().Be(false);
        }

        [Test]
        public void When_replay_a_handle_and_replay_event()
        {
            SynchronousBus.Replay(new HandleAndReplayEvent());

            //Assert
            Container.Resolve<MessageHandler>().ReceiveAMessage.Should().Be(false);
            Container.Resolve<MessageHandler>().ReceiveInProcessMessage.Should().Be(false);
            Container.Resolve<MessageHandler>().ReceiveRemoteMessage.Should().Be(false);
            Container.Resolve<MessageHandler>().ReceiveReplayEvent.Should().Be(false);
            Container.Resolve<MessageHandler>().ReceiveHandleAndReplayEvent.Should().Be(true);
        }
    }
}
