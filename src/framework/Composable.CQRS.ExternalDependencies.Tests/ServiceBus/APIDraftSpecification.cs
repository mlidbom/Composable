using System;
using System.Threading.Tasks;
using Composable.DDD;
using Composable.DependencyInjection;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Composable.Messaging.Commands;
using Composable.Persistence.EventStore;
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
                host.RegisterAndStartEndpoint(
                    "Backend",
                    endpointBuilder =>
                    {
                        QueryResult queryResult = null;

                        endpointBuilder.RegisterHandler
                                       .ForCommand((MyCommand command) => endpointBuilder.Container.CreateServiceLocator().Resolve<IServiceBus>().Publish(new MyEvent()))
                                       .ForEvent((MyEvent @event) => queryResult = new QueryResult())
                                       .ForQuery((MyQuery query) => queryResult);
                    });

                var clientEndpoint = host.RegisterAndStartEndpoint("client", _ => {});

                var clientBus = clientEndpoint.ServiceLocator.Resolve<IServiceBus>();

                clientBus.Send(new MyCommand());

                var result = clientBus.Query(new MyQuery());
                result.Should().NotBeNull();

                result = await clientBus.QueryAsync(new MyQuery());
                result.Should().NotBeNull();
            }
        }
    }

    class MyEvent : AggregateRootEvent {}
    class QueryResult : IQueryResult, IHasPersistentIdentity<Guid>
    {
        public Guid Id { get; set; }
    }
    class MyQuery : IQuery<QueryResult> {}
    class MyCommand : Command {}
}
