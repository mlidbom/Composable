using System;
using System.Threading.Tasks;
using Composable.DDD;
using Composable.DependencyInjection;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Composable.Messaging.Commands;
using Composable.Messaging.Events;
using Composable.Persistence.EventStore;
using FluentAssertions;
using Xunit;

namespace Composable.CQRS.Tests.ServiceBusSpecification
{
    public class APIDraftSpecification
    {
        [Fact] async Task SettingUpAHost()
        {
            using(var host = EndpointHost.Testing.CreateHost())
            {
                host.RegisterAndStartEndpoint(
                    "Backend",
                    endpointBuilder =>
                    {
                        MyQueryResult queryResult = null;

                        endpointBuilder.RegisterHandlers
                                       .ForCommand((MyCommand command, IServiceBus bus) => bus.Publish(new MyEvent()))
                                       .ForEvent((MyEvent @event) => queryResult = new MyQueryResult())
                                       .ForQuery((MyQuery query) => queryResult)
                                       .ForCommand((MyCommandWithResult command) => new MyCommandResult());
                    });

                host.ClientBus.Send(new MyCommand());

                var result = host.ClientBus.Query(new MyQuery());
                result.Should().NotBeNull();

                result = await host.ClientBus.QueryAsync(new MyQuery());
                result.Should().NotBeNull();

                var commandResult = await host.ClientBus.SendAsync(new MyCommandWithResult());
                commandResult.Should().NotBe(null);

            }
        }

        class MyEvent : Event { }
        class MyQueryResult : QueryResult { }
        class MyQuery : Query<MyQueryResult> { }
        class MyCommand : Command { }
        class MyCommandWithResult : Command<MyCommandResult> { }
        class MyCommandResult : Message { }
    }
}
