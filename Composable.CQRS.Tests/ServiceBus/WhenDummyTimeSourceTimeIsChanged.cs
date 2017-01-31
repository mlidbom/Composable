using System;
using System.Collections.Generic;
using Castle.MicroKernel.Lifestyle;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.CQRS.EventSourcing;
using Composable.DDD;
using Composable.GenericAbstractions.Time;
using Composable.ServiceBus;
using Composable.System.Reflection;
using Composable.Windsor.Testing;
using FluentAssertions;
using NUnit.Framework;

namespace CQRS.Tests.ServiceBus
{
    using Composable.System;

    [TestFixture]
    public class WhenDummyTimeSourceTimeIsChanged
    {
        private IServiceBus _bus;
        private MessageReceiver _messageReceiver;
        private DummyTimeSource _timeSource;
        private IDisposable _scope;
        private WindsorContainer _container;
        [SetUp]
        public void SetupTask()
        {
            _container = new WindsorContainer();

            _container.ConfigureWiringForTestsCallBeforeAllOtherWiring();

            _messageReceiver = new MessageReceiver();
            _timeSource = DummyTimeSource.FromLocalTime(DateTime.Parse("2015-01-01 10:00"));

            _container.Register(
                Component.For<DummyTimeSource>().Instance(_timeSource),
                Component.For<IServiceBus>().ImplementedBy<TestingOnlyServiceBus>().LifestyleScoped(),
                Component.For<IWindsorContainer>().Instance(_container),
                Component.For<IHandleMessages<ScheduledMessage>>().Instance(_messageReceiver)
                );

            _container.ConfigureWiringForTestsCallAfterAllOtherWiring();

            _scope = _container.BeginScope();

            _bus = _container.Resolve<IServiceBus>();
        }

        [Test]
        public void DueMessagesAreDelivered()
        {
            var now = _timeSource.UtcNow;
            var inOneHour = new ScheduledMessage();
            _bus.SendAtTime(now + 1.Hours(), inOneHour);

            _timeSource.UtcNow = now + 1.Hours();

            _messageReceiver.ReceivedMessages.Should().Contain(inOneHour);
        }

        [Test]
        public void NotDueMessagesAreNotDelivered()
        {
            var now = _timeSource.UtcNow;
            var inOneHour = new ScheduledMessage();
            _bus.SendAtTime(now + 1.Hours(), inOneHour);

            _timeSource.UtcNow = now + 1.Minutes();

            _messageReceiver.ReceivedMessages.Should().BeEmpty();
        }

        [TearDown]
        public void TearDownTask()
        {
            _scope.Dispose();
            _container.Dispose();
        }

        public class ScheduledMessage : ValueObject<ScheduledMessage>, IEvent
        {
        }

        public class MessageReceiver : IHandleMessages<ScheduledMessage>
        {
            private readonly List<ScheduledMessage> _receivedMessages = new List<ScheduledMessage>();
            public IReadOnlyList<ScheduledMessage> ReceivedMessages => _receivedMessages;

            public void Handle(ScheduledMessage message) { _receivedMessages.Add(message); }
        }
    }
}
