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
                                                     RegisterDomainComponents(builder.Container, builder.Configuration, builder.RegisterHandlers);
                                                     RegisterHandlers(builder.RegisterHandlers);
                                                 });
        }

        void RegisterDomainComponents(IDependencyInjectionContainer container, EndpointConfiguration configuration, MessageHandlerRegistrarWithDependencyInjectionSupport registrar)
        {
            container.RegisterSqlServerEventStore(configuration.ConnectionStringName)
                                   .HandleAggregate<Account, AccountEvent.Root>(registrar);

            container.RegisterSqlServerDocumentDb(configuration.ConnectionStringName)
                                   .HandleDocumentType<EventStoreApi.Query.AggregateLink<Account>>(registrar);
        }

        void RegisterHandlers(MessageHandlerRegistrarWithDependencyInjectionSupport registrar)
        {
            UIAdapterLayer.Register(registrar);

            AccountQueryModel.Api.RegisterHandlers(registrar);

            EmailToAccountMapper.UpdateMappingWhenEmailChanges(registrar);
            EmailToAccountMapper.TryGetAccountByEmail(registrar);
        }
    }
}
