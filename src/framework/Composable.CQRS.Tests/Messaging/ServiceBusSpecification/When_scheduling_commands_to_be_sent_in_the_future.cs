using System;
using System.Threading.Tasks;
using Composable.DependencyInjection;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Buses;
using Composable.Messaging.Commands;
using Composable.System;
using Composable.Testing.Threading;
using FluentAssertions;
using Xunit;

namespace Composable.Tests.Messaging.ServiceBusSpecification
{
    public class When_scheduling_commands_to_be_sent_in_the_future : IDisposable
    {
        readonly IServiceBus _bus;
        readonly IUtcTimeTimeSource _timeSource;
        readonly IThreadGate _receivedCommandGate;
        readonly ITestingEndpointHost _host;

        public When_scheduling_commands_to_be_sent_in_the_future()
        {
            _host = EndpointHost.Testing.CreateHost(DependencyInjectionContainer.Create);

            var endpoint = _host.RegisterAndStartEndpoint(
                "endpoint",
                new EndpointId(Guid.Parse("17ED9DF9-33A8-4DF8-B6EC-6ED97AB2030B")),
                builder => builder.RegisterHandlers.ForCommand<ScheduledCommand>(
                    cmd => _receivedCommandGate.AwaitPassthrough()));

            var serviceLocator = endpoint.ServiceLocator;
            _receivedCommandGate = ThreadGate.CreateOpenWithTimeout(TimeSpanExtensions.Seconds(1));

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
            _bus.SendAtTime(now + TimeSpanExtensions.Seconds(2), inOneHour);

            _receivedCommandGate.TryAwaitPassededThroughCountEqualTo(1, timeout: .5.Seconds())
                                .Should().Be(false);
        }

        public void Dispose() { _host.Dispose(); }

        [TypeId("BEB1E3BA-3515-43EA-A33D-1EE7A5775A11")]class ScheduledCommand : TransactionalExactlyOnceDeliveryCommand {}
    }
}
