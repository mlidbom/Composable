using System;
using System.Threading;
using Composable.DependencyInjection;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Buses;
using Composable.Messaging.Commands;
using Composable.System;
using Composable.Testing.Threading;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.CQRS.Tests.ServiceBusSpecification
{
    using Composable.System;

    [TestFixture]
    public class WhenDummyTimeSourceTimeIsChanged
    {
        IServiceBus _bus;
        DummyTimeSource _timeSource;
        IDisposable _scope;
        IServiceLocator _serviceLocator;
        IThreadGate _receivedCommandGate = null;

        [SetUp]
        public void SetupTask()
        {
            _serviceLocator = DependencyInjectionContainer.CreateServiceLocatorForTesting(cont => {});
            _receivedCommandGate = ThreadGate.CreateOpenWithTimeout(1.Seconds());

            _timeSource = _serviceLocator.Resolve<DummyTimeSource>();
            _timeSource.UtcNow = DateTime.Parse("2015-01-01 10:00");
            _scope = _serviceLocator.BeginScope();

            _bus = _serviceLocator.Resolve<IServiceBus>();
            _serviceLocator.Resolve<IMessageHandlerRegistrar>()
                      .ForCommand<ScheduledCommand>(cmd => _receivedCommandGate.AwaitPassthrough());
        }

        [Test]
        public void DueMessagesAreDelivered()
        {
            var now = _timeSource.UtcNow;
            var inOneHour = new ScheduledCommand();
            _bus.SendAtTime(now + 1.Hours(), inOneHour);

            _timeSource.UtcNow = now + 1.Hours();

            _receivedCommandGate.AwaitPassedThroughCountEqualTo(1);
        }

        [Test]
        public void NotDueMessagesAreNotDelivered()
        {
            var now = _timeSource.UtcNow;
            var inOneHour = new ScheduledCommand();
            _bus.SendAtTime(now + 1.Hours(), inOneHour);

            _timeSource.UtcNow = now + 1.Minutes();

            Thread.Sleep(10.Milliseconds());

            _receivedCommandGate.Passed.Should().Be(0);
        }

        [TearDown]
        public void TearDownTask()
        {
            _scope.Dispose();
            _serviceLocator.Dispose();
        }

        class ScheduledCommand : Command
        {
        }
    }
}
