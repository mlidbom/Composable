using System;
using System.Threading.Tasks;
using Composable.DependencyInjection;
using Composable.GenericAbstractions.Time;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Composable.Persistence.Common.DependencyInjection;
using Composable.SystemCE;
using Composable.Testing;
using Composable.Testing.Threading;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.Messaging.ServiceBusSpecification
{
    public class When_scheduling_commands_to_be_sent_in_the_future : DuplicateByPluggableComponentTest
    {
        IUtcTimeTimeSource _timeSource;
        IThreadGate _receivedCommandGate;
        ITestingEndpointHost _host;
        IEndpoint _endpoint;

        [SetUp]public async Task Setup()
        {
            _host = TestingEndpointHost.Create(DependencyInjectionContainer.Create);

            _endpoint = _host.RegisterEndpoint(
                "endpoint",
                new EndpointId(Guid.Parse("17ED9DF9-33A8-4DF8-B6EC-6ED97AB2030B")),
                builder =>
                {
                    builder.RegisterCurrentTestsConfiguredPersistenceLayer();
                    builder.RegisterHandlers.ForCommand<ScheduledCommand>(
                        cmd => _receivedCommandGate.AwaitPassThrough());

                    builder.TypeMapper.Map<ScheduledCommand>("6bc9abe2-8861-4108-98dd-8aa1b50c0c42");
                });

            await _host.StartAsync();

            var serviceLocator = _endpoint.ServiceLocator;
            _receivedCommandGate = ThreadGate.CreateOpenWithTimeout(1.Seconds());
            _timeSource = serviceLocator.Resolve<IUtcTimeTimeSource>();
        }

        [Test] public void Messages_whose_due_time_has_passed_are_delivered()
        {
            var now = _timeSource.UtcNow;
            var inOneHour = new ScheduledCommand();

            _endpoint.ExecuteServerRequestInTransaction(session => session.ScheduleSend(now + .2.Seconds(), inOneHour));

            _receivedCommandGate.AwaitPassedThroughCountEqualTo(1, timeout: 2.Seconds());
        }

        [Test] public void Messages_whose_due_time_have_not_passed_are_not_delivered()
        {
            var now = _timeSource.UtcNow;
            var inOneHour = new ScheduledCommand();
            _endpoint.ExecuteServerRequestInTransaction(session => session.ScheduleSend(now + 2.Seconds(), inOneHour));

            _receivedCommandGate.TryAwaitPassededThroughCountEqualTo(1, timeout: .5.Seconds())
                                .Should().Be(false);
        }

        [TearDown]public void TearDown()
        {
            _host.Dispose();
        }

        class ScheduledCommand : MessageTypes.Remotable.ExactlyOnce.Command {}

        public When_scheduling_commands_to_be_sent_in_the_future(string _) : base(_) {}
    }
}
