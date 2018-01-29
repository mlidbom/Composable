using System;
using Composable.DependencyInjection;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Composable.Persistence.EventStore;
using NUnit.Framework;

namespace Composable.Tests.Messaging.ServiceBusSpecification.Performance
{
    public class PerformanceTestBase
    {
        protected ITestingEndpointHost Host;
        protected IEndpoint ServerEndpoint;
        public IEndpoint ClientEndpoint;
        protected IRemoteApiNavigatorSession RemoteNavigator => Host.RemoteNavigator;
        protected IServiceBusSession ServerBusSession => ServerEndpoint.ServiceLocator.Resolve<IServiceBusSession>();

        [SetUp] public void Setup()
        {
            Host = EndpointHost.Testing.CreateHost(DependencyInjectionContainer.Create);
            ClientEndpoint = Host.ClientEndpoint;
            ServerEndpoint = Host.RegisterAndStartEndpoint(
                "Backend",
                new EndpointId(Guid.Parse("DDD0A67C-D2A2-4197-9AF8-38B6AEDF8FA6")),
                builder =>
                {
                    builder.RegisterHandlers
                           .ForCommand((MyCommand command) => {})
                           .ForEvent((MyEvent myEvent) => {})
                           .ForQuery((MyRemoteQuery query) => new MyQueryResult())
                           .ForQuery((MyLocalQuery query) => new MyQueryResult())
                           .ForCommandWithResult((MyCommandWithResult command) => new MyCommandResult());

                    builder.TypeMapper.Map<MyCommand>("0ddefcaa-4d4d-48b2-9e1a-762c0b835275")
                           .Map<MyCommandWithResult>("24248d03-630b-4909-a6ea-e7fdaf82baa2")
                           .Map<MyEvent>("2fdde21f-c6d4-46a2-95e5-3429b820dfc3")
                           .Map<MyRemoteQuery>("b9d62f22-514b-4e3c-9ac1-66940a7a8144")
                           .Map<MyLocalQuery>("5640cfb1-0dbc-4e2b-9915-b5b91a289e86");
                });
        }

        [TearDown] public void TearDown() { Host.Dispose(); }

        protected class MyCommand : BusApi.Remotable.ExactlyOnce.Command {}
        protected class MyEvent : AggregateEvent {}
        protected class MyRemoteQuery : BusApi.Remotable.NonTransactional.Queries.Query<MyQueryResult> {}
        protected class MyLocalQuery : BusApi.StrictlyLocal.Queries.Query<MyQueryResult> {}
        protected class MyQueryResult {}
        protected class MyCommandWithResult : BusApi.Remotable.AtMostOnce.Command<MyCommandResult> {}
        protected class MyCommandResult {}
    }
}
