using System;
using AccountManagement.API;
using AccountManagement.Domain;
using AccountManagement.Domain.QueryModels;
using AccountManagement.Domain.Services;
using AccountManagement.UI.QueryModels;
using AccountManagement.UI.QueryModels.Services;
using AccountManagement.UI.QueryModels.Services.Implementation;
using Composable.DependencyInjection;
using Composable.DependencyInjection.Persistence;
using Composable.Messaging.Buses;
using Composable.Messaging.Buses.Implementation;
using Composable.SystemExtensions.Threading;

namespace AccountManagement
{
    public static class AccountManagementServerDomainBootstrapper
    {

        public static IEndpoint RegisterWith(IEndpointHost host)
        {
            return host.RegisterAndStartEndpoint("AccountManagement",
                                                 new EndpointId(Guid.Parse("1A1BE9C8-C8F6-4E38-ABFB-F101E5EDB00D")),
                                                 builder =>
                                                 {
                                                     RegisterDomainComponents(builder.Container, builder.Configuration);
                                                     RegisterUserInterfaceComponents(builder.Container, builder.Configuration);

                                                     RegisterHandlers(builder.RegisterHandlers);
                                                 });
        }

        static void RegisterDomainComponents(IDependencyInjectionContainer container, EndpointConfiguration configuration)
        {
            container.RegisterSqlServerEventStore<IAccountManagementEventStoreUpdater, IAccountManagementEventStoreReader>(configuration.ConnectionStringName);

            container.RegisterSqlServerDocumentDb<IAccountManagementDomainDocumentDbUpdater, IAccountManagementDomainDocumentDbReader, IAccountManagementDomainDocumentDbBulkReader>(configuration.ConnectionStringName);

            container.Register(
                Component.For<IAccountRepository>()
                         .UsingFactoryMethod((IAccountManagementEventStoreUpdater aggregates, IAccountManagementEventStoreReader reader) => new AccountRepository(aggregates, reader))
                         .LifestyleScoped(),
                Component.For<IFindAccountByEmail>()
                         .UsingFactoryMethod((IAccountManagementDomainDocumentDbReader queryModels) => new FindAccountByEmail(queryModels))
                         .LifestyleScoped());
        }

        static void RegisterUserInterfaceComponents(IDependencyInjectionContainer container, EndpointConfiguration configuration)
        {
            container.RegisterSqlServerDocumentDb<IAccountManagementUiDocumentDbUpdater, IAccountManagementUiDocumentDbReader, IAccountManagementUiDocumentDbBulkReader>(configuration.ConnectionStringName);

            container.Register(
                Component.For<AccountManagementQueryModelReader>()
                         .UsingFactoryMethod((IAccountManagementUiDocumentDbReader documentDbQueryModels, AccountQueryModel.Generator accountQueryModelGenerator, ISingleContextUseGuard usageGuard) =>
                                                 new AccountManagementQueryModelReader(documentDbQueryModels, accountQueryModelGenerator, usageGuard))
                         .LifestyleScoped());

            container.Register(Component.For<AccountQueryModel.Generator>()
                                        .UsingFactoryMethod((IAccountManagementEventStoreReader session) => new AccountQueryModel.Generator(session))
                                        .LifestyleScoped());
        }

        static void RegisterHandlers(MessageHandlerRegistrarWithDependencyInjectionSupport registrar)
        {
            registrar.ForQuery((StartResourceQuery query) =>
                                   new StartResource());

            EmailToAccountMapQueryModel.RegisterHandlers(registrar);
            EmailToAccountIdQueryModel.RegisterHandlers(registrar);

            Account.MessageHandlers.RegisterHandlers(registrar);
        }
    }
}
