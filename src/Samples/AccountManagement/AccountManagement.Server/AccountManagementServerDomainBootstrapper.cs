using System;
using AccountManagement.Domain;
using AccountManagement.Domain.Events;
using AccountManagement.UI;
using AccountManagement.UI.QueryModels;
using Composable.DependencyInjection;
using Composable.DependencyInjection.Persistence;
using Composable.Messaging.Buses;
using Composable.Messaging.Buses.Implementation;
using Composable.Persistence.EventStore;

namespace AccountManagement
{
    public class AccountManagementServerDomainBootstrapper
    {
        public IEndpoint RegisterWith(IEndpointHost host)
        {
            return host.RegisterAndStartEndpoint(name: "AccountManagement",
                                                 id: new EndpointId(Guid.Parse(input: "1A1BE9C8-C8F6-4E38-ABFB-F101E5EDB00D")),
                                                 setup: builder =>
                                                 {
                                                     TypeMapper.MapTypes(builder.TypeMapper);
                                                     RegisterDomainComponents(builder);
                                                     RegisterHandlers(builder);
                                                 });
        }

        static void RegisterDomainComponents(IEndpointBuilder builder)
        {
            builder.Container.RegisterSqlServerEventStore(builder.Configuration.ConnectionStringName)
                                   .HandleAggregate<Account, AccountEvent.Root>(builder.RegisterHandlers);

            builder.Container.RegisterSqlServerDocumentDb(builder.Configuration.ConnectionStringName)
                                   .HandleDocumentType<EventStoreApi.Query.AggregateLink<Account>>(builder.RegisterHandlers);
        }

        static void RegisterHandlers(IEndpointBuilder builder)
        {
            UIAdapterLayer.Register(builder.RegisterHandlers);

            AccountQueryModel.Api.RegisterHandlers(builder.RegisterHandlers);

            EmailToAccountMapper.UpdateMappingWhenEmailChanges(builder.RegisterHandlers);
            EmailToAccountMapper.TryGetAccountByEmail(builder.RegisterHandlers);
        }
    }
}
