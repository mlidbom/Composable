using System;
using AccountManagement.Domain;
using AccountManagement.Domain.Events;
using AccountManagement.UI;
using AccountManagement.UI.QueryModels;
using Composable.DependencyInjection.Persistence;
using Composable.Messaging.Buses;
using Composable.Persistence.EventStore;

namespace AccountManagement
{
    public class AccountManagementServerDomainBootstrapper
    {
        public IEndpoint RegisterWith(IEndpointHost host)
        {
            return host.RegisterEndpoint(name: "AccountManagement",
                                                 id: new EndpointId(Guid.Parse(input: "1A1BE9C8-C8F6-4E38-ABFB-F101E5EDB00D")),
                                                 setup: builder =>
                                                 {
                                                     AccountManagementApiTypeMapper.MapTypes(builder.TypeMapper);
                                                     DomainTypeMapper.MapTypes(builder.TypeMapper);
                                                     RegisterDomainComponents(builder);
                                                     RegisterHandlers(builder);
                                                 });
        }

        static void RegisterDomainComponents(IEndpointBuilder builder)
        {
            builder.RegisterSqlServerPersistenceLayer();
            builder.RegisterSqlServerEventStore()
                   .HandleAggregate<Account, AccountEvent.Root>(builder.RegisterHandlers);

            builder.RegisterSqlServerDocumentDb()
                   .HandleDocumentType<EventStoreApi.Query.AggregateLink<Account>>(builder.RegisterHandlers)
                   .HandleDocumentType<AccountStatistics.SingletonStatisticsQuerymodel>(builder.RegisterHandlers);
        }

        static void RegisterHandlers(IEndpointBuilder builder)
        {
            UIAdapterLayer.Register(builder.RegisterHandlers);

            //todo: This should not be called synchronously. We should have it in a separate consistency boundary so that it does not slow down every operation on an account.
            AccountStatistics.Register(builder);

            AccountQueryModel.Api.RegisterHandlers(builder.RegisterHandlers);

            EmailToAccountMapper.UpdateMappingWhenEmailChanges(builder.RegisterHandlers);
            EmailToAccountMapper.TryGetAccountByEmail(builder.RegisterHandlers);
        }
    }
}
