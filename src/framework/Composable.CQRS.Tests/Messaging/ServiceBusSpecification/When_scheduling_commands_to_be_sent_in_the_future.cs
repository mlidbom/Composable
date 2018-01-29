using System;
using Composable.DependencyInjection;
using Composable.GenericAbstractions.Time;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Composable.System;
using Composable.Testing.Threading;
using FluentAssertions;
using Xunit;

namespace Composable.Tests.Messaging.ServiceBusSpecification
{
    public class When_scheduling_commands_to_be_sent_in_the_future : IDisposable
    {
        readonly IApiBrowser _busSession;
        readonly IUtcTimeTimeSource _timeSource;
        readonly IThreadGate _receivedCommandGate;
        readonly ITestingEndpointHost _host;
        IDisposable _scope;

        public When_scheduling_commands_to_be_sent_in_the_future()
        {
            _host = EndpointHost.Testing.CreateHost(DependencyInjectionContainer.Create);

            var endpoint = _host.RegisterAndStartEndpoint(
                "endpoint",
                new EndpointId(Guid.Parse("17ED9DF9-33A8-4DF8-B6EC-6ED97AB2030B")),
                builder =>
                {
                    builder.RegisterHandlers.ForCommand<ScheduledCommand>(
                        cmd => _receivedCommandGate.AwaitPassthrough());

                    builder.TypeMapper.Map<ScheduledCommand>("6bc9abe2-8861-4108-98dd-8aa1b50c0c42");
                });

            var serviceLocator = endpoint.ServiceLocator;
            _receivedCommandGate = ThreadGate.CreateOpenWithTimeout(TimeSpanExtensions.Seconds(1));

            _timeSource = serviceLocator.Resolve<IUtcTimeTimeSource>();
            _scope = serviceLocator.BeginScope();
            _busSession = serviceLocator.Resolve<IApiBrowser>();
        }

        [Fact] public void Messages_whose_due_time_has_passed_are_delivered()
        {
            var now = _timeSource.UtcNow;
            var inOneHour = new ScheduledCommand();
            _busSession.SchedulePostRemote(now + .1.Seconds(), inOneHour);

            _receivedCommandGate.AwaitPassedThroughCountEqualTo(1, timeout: .5.Seconds());
        }

        [Fact] public void Messages_whose_due_time_have_not_passed_are_not_delivered()
        {
            var now = _timeSource.UtcNow;
            var inOneHour = new ScheduledCommand();
            _busSession.SchedulePostRemote(now + TimeSpanExtensions.Seconds(2), inOneHour);

            _receivedCommandGate.TryAwaitPassededThroughCountEqualTo(1, timeout: .5.Seconds())
                                .Should().Be(false);
        }

        public void Dispose()
        {
            _scope.Dispose();
            _host.Dispose();
        }

        class ScheduledCommand : BusApi.Remote.ExactlyOnce.Command {}
    }
}
