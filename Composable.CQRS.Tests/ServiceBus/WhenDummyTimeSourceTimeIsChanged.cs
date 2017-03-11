using System;
using Castle.MicroKernel.Lifestyle;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Buses;
using Composable.Messaging.Commands;
using Composable.System;
using Composable.Windsor.Testing;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.CQRS.Tests.ServiceBus
{
    [TestFixture]
    public class WhenDummyTimeSourceTimeIsChanged
    {
        IServiceBus _bus;
        DummyTimeSource _timeSource;
        IDisposable _scope;
        WindsorContainer _container;
        ScheduledCommand _receivedCommand = null;

        [SetUp]
        public void SetupTask()
        {
            _container = new WindsorContainer();
            _receivedCommand = null;

            _container.ConfigureWiringForTestsCallBeforeAllOtherWiring();

            _timeSource = DummyTimeSource.FromLocalTime(DateTime.Parse("2015-01-01 10:00"));

          _container.Register(
                              Component.For<DummyTimeSource>()
                                       .Instance(_timeSource),
                              Component.For<IMessageHandlerRegistrar, IMessageHandlerRegistry>()
                                       .ImplementedBy<MessageHandlerRegistry>()
                                       .LifestyleSingleton(),
                              Component.For<IServiceBus>()
                                       .ImplementedBy<TestingOnlyServiceBus>()
                                       .LifestyleScoped());

            _container.ConfigureWiringForTestsCallAfterAllOtherWiring();

            _scope = _container.BeginScope();

            _bus = _container.Resolve<IServiceBus>();
            _container.Resolve<IMessageHandlerRegistrar>()
                      .ForCommand<ScheduledCommand>(cmd => _receivedCommand = cmd);
        }

        [Test]
        public void DueMessagesAreDelivered()
        {
            var now = _timeSource.UtcNow;
            var inOneHour = new ScheduledCommand();
            _bus.SendAtTime(now + TimeSpanExtensions.Hours(1), inOneHour);

            _timeSource.UtcNow = now + TimeSpanExtensions.Hours(1);

            _receivedCommand.Should()
                           .Be(inOneHour);
        }

        [Test]
        public void NotDueMessagesAreNotDelivered()
        {
            var now = _timeSource.UtcNow;
            var inOneHour = new ScheduledCommand();
            _bus.SendAtTime(now + TimeSpanExtensions.Hours(1), inOneHour);

            _timeSource.UtcNow = now + TimeSpanExtensions.Minutes(1);

            _receivedCommand.Should()
                           .Be(null);
        }

        [TearDown]
        public void TearDownTask()
        {
            _scope.Dispose();
            _container.Dispose();
        }

        class ScheduledCommand : Command
        {
        }
    }
}
