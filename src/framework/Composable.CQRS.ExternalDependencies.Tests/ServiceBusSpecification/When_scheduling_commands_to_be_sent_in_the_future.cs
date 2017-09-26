using System;
using Composable.DependencyInjection;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Buses;
using Composable.Messaging.Commands;
using Composable.Testing.Threading;
using FluentAssertions;
using Xunit;

namespace Composable.CQRS.Tests.ServiceBusSpecification
{
    using Composable.System;

    public class When_scheduling_commands_to_be_sent_in_the_future : IDisposable
    {
        readonly IServiceBus _bus;
        readonly IUtcTimeTimeSource _timeSource;
        readonly IThreadGate _receivedCommandGate;
        readonly ITestingEndpointHost _host;

        public When_scheduling_commands_to_be_sent_in_the_future()
        {
            _host = EndpointHost.Testing.CreateHost();

            var endpoint = _host.RegisterAndStartEndpoint(
                "endpoint",
                builder => builder.RegisterHandler.ForCommand<ScheduledCommand>(
                    cmd => _receivedCommandGate.AwaitPassthrough()));

            var serviceLocator = endpoint.ServiceLocator;
            _receivedCommandGate = ThreadGate.CreateOpenWithTimeout(1.Seconds());

            _timeSource = serviceLocator.Resolve<IUtcTimeTimeSource>();
            _bus = serviceLocator.Resolve<IServiceBus>();
        }

        [Fact] public void Messages_whose_due_time_has_passed_are_delivered()
        {
            var now = _timeSource.UtcNow;
            var inOneHour = new ScheduledCommand();
            _bus.SendAtTime(now + .1.Seconds(), inOneHour);

            _receivedCommandGate.AwaitPassedThroughCountEqualTo(1, timeout: .5.Seconds());
        }

        [Fact] public void Messages_whose_due_time_have_not_passed_are_not_delivered()
        {
            var now = _timeSource.UtcNow;
            var inOneHour = new ScheduledCommand();
            _bus.SendAtTime(now + 2.Seconds(), inOneHour);

            _receivedCommandGate.TryAwaitPassededThroughCountEqualTo(1, timeout: .5.Seconds())
                                .Should().Be(false);
        }

        public void Dispose() { _host.Dispose(); }

        class ScheduledCommand : Command {}
    }
}
