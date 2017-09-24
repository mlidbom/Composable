using System.Threading.Tasks;
using Composable.DependencyInjection;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Composable.Messaging.Commands;
using Composable.Testing.Threading;
using FluentAssertions;
using Xunit;

namespace Composable.CQRS.Tests.ServiceBus
{
    public class APIDraftSpecification
    {
        [Fact] async Task SettingUpAHost()
        {
            using(var host = EndpointHost.Testing.CreateHost())
            {
                var commandReceivedGate = ThreadGate.CreateOpenWithTimeout(10.Milliseconds());
                var eventReceivedGate = ThreadGate.CreateOpenWithTimeout(10.Milliseconds());

                host.RegisterEndpoint(endpointBuilder =>
                {
                    endpointBuilder.MessageHandlerRegistrar.RegisterCommandHandler((MyCommand command) =>
                    {
                        commandReceivedGate.AwaitPassthrough();
                        endpointBuilder.Container.CreateServiceLocator().Resolve<IInterProcessServiceBus>().Publish(new MyEvent());
                    });
                    endpointBuilder.MessageHandlerRegistrar.RegisterQueryHandler((MyQuery query) => new QueryResult());
                });

                var clientEndpoint = host.RegisterEndpoint(endpointBuilder => endpointBuilder.MessageHandlerRegistrar.RegisterEventHandler((MyEvent @event) => eventReceivedGate.AwaitPassthrough()));

                var clientBus = clientEndpoint.ServiceLocator.Resolve<IInterProcessServiceBus>();

                clientBus.Send(new MyCommand());

                commandReceivedGate.AwaitPassedThroughCountEqualTo(1);
                eventReceivedGate.AwaitPassedThroughCountEqualTo(1);

                var result = clientBus.Query(new MyQuery());
                result.Should().NotBeNull();

                result = await clientBus.QueryAsync(new MyQuery());
                result.Should().NotBeNull();
            }
        }
    }

    class MyEvent : IEvent {}
    class QueryResult : IQueryResult {}
    class MyQuery : IQuery<QueryResult> {}
    class MyCommand : Command {}
}
