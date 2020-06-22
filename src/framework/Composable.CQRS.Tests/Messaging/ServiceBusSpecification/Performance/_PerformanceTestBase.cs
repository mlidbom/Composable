using System;
using System.Threading.Tasks;
using Composable.DependencyInjection;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Composable.Messaging.Hypermedia;
using NCrunch.Framework;
using NUnit.Framework;

//ncrunch: no coverage start

namespace Composable.Tests.Messaging.ServiceBusSpecification.Performance
{
    [TestFixture, Performance, Serial] public class PerformanceTestBase
    {
        protected ITestingEndpointHost Host;
        protected IEndpoint ServerEndpoint;
        public IEndpoint ClientEndpoint;
        protected IRemoteHypermediaNavigator RemoteNavigator => ClientEndpoint.ServiceLocator.Resolve<IRemoteHypermediaNavigator>();
        protected IServiceBusSession ServerBusSession => ServerEndpoint.ServiceLocator.Resolve<IServiceBusSession>();

        [SetUp] public async Task Setup()
        {
            Host = SqlServerTestingEndpointHost.Create(DependencyInjectionContainer.Create, TestingMode.DatabasePool);
            ServerEndpoint = Host.RegisterEndpoint(
                "Backend",
                new EndpointId(Guid.Parse("DDD0A67C-D2A2-4197-9AF8-38B6AEDF8FA6")),
                builder =>
                {
                    builder.RegisterSqlServerPersistenceLayer();
                    builder.RegisterHandlers
                           .ForQuery((MyRemoteQuery query) => new MyQueryResult())
                           .ForQuery((MyLocalQuery query) => new MyQueryResult());

                    builder.TypeMapper
                           .Map<MyRemoteQuery>("b9d62f22-514b-4e3c-9ac1-66940a7a8144")
                           .Map<MyLocalQuery>("5640cfb1-0dbc-4e2b-9915-b5b91a289e86")
                           .Map<MyQueryResult>("07e144ab-af3c-4c2c-9d83-492deffd24aa");
                });

            ClientEndpoint = Host.RegisterClientEndpointForRegisteredEndpoints();
            await Host.StartAsync();
        }

        [TearDown] public void TearDown() { Host.Dispose(); }

        protected class MyRemoteQuery : MessageTypes.Remotable.NonTransactional.Queries.Query<MyQueryResult> {}
        protected class MyLocalQuery : MessageTypes.StrictlyLocal.Queries.Query<MyQueryResult> {}
        protected class MyQueryResult {}
    }
}
