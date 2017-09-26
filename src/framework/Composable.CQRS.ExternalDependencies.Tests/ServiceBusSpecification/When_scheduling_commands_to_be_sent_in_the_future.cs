using Composable.DependencyInjection;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Buses;
using Composable.Messaging.Commands;

using Composable.Testing.Threading;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.CQRS.Tests.ServiceBusSpecification
{
    using Composable.System;

    [TestFixture] public class When_scheduling_commands_to_be_sent_in_the_future
    {
        IServiceBus _bus;
        IUtcTimeTimeSource _timeSource;
        IServiceLocator _serviceLocator;
        IThreadGate _receivedCommandGate = null;
        ITestingEndpointHost _host;

        [SetUp] public void SetupTask()
        {
            _host = EndpointHost.Testing.CreateHost();

            var endpoint = _host.RegisterAndStartEndpoint(
                "endpoint",
                builder => builder.RegisterHandler.ForCommand<ScheduledCommand>(
                    cmd => _receivedCommandGate.AwaitPassthrough()));

            _serviceLocator = endpoint.ServiceLocator;
            _receivedCommandGate = ThreadGate.CreateOpenWithTimeout(1.Seconds());

            _timeSource = _serviceLocator.Resolve<IUtcTimeTimeSource>();
            _bus = _serviceLocator.Resolve<IServiceBus>();
        }

        [Test] public void Messages_whose_due_time_has_passed_are_delivered()
        {
            var now = _timeSource.UtcNow;
            var inOneHour = new ScheduledCommand();
            _bus.SendAtTime(now + .1.Seconds(), inOneHour);

            _receivedCommandGate.AwaitPassedThroughCountEqualTo(1, timeout: .5.Seconds());
        }

        [Test] public void Messages_whose_due_time_have_not_passed_are_not_delivered()
        {
            var now = _timeSource.UtcNow;
            var inOneHour = new ScheduledCommand();
            _bus.SendAtTime(now + 2.Seconds(), inOneHour);

            _receivedCommandGate.TryAwaitPassededThroughCountEqualTo(1, timeout: .5.Seconds())
                .Should().Be(false);
        }

        [TearDown] public void TearDownTask() { _host.Dispose(); }

        class ScheduledCommand : Command {}
    }
}
