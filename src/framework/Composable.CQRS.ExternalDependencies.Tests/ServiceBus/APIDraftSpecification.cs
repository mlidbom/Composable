using System;
using System.Threading;
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
                host.RegisterEndpoint("Backend", endpointBuilder =>
                {
                    endpointBuilder.MessageHandlerRegistrar.RegisterCommandHandler(
                        (MyCommand command) => endpointBuilder.Container.CreateServiceLocator().Resolve<IInterProcessServiceBus>().Publish(
                            new MyEvent()));

                    QueryResult queryResult = null;
                    endpointBuilder.MessageHandlerRegistrar.RegisterEventHandler((MyEvent @event) => queryResult = new QueryResult());

                    endpointBuilder.MessageHandlerRegistrar.RegisterQueryHandler((MyQuery query) => queryResult);
                });

                var clientEndpoint = host.RegisterEndpoint("client", _ => {});

                host.Start();

                var clientBus = clientEndpoint.ServiceLocator.Resolve<IInterProcessServiceBus>();

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
