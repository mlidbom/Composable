using AccountManagement.Domain;
using AccountManagement.Domain.Events;
using AccountManagement.Domain.QueryModels.Updaters;
using AccountManagement.Domain.Services;
using AccountManagement.UI.QueryModels.EventStoreGenerated;
using AccountManagement.UI.QueryModels.Services;
using AccountManagement.UI.QueryModels.Services.Implementation;
using Composable.DependencyInjection;
using Composable.DependencyInjection.Persistence;
using Composable.Messaging.Buses;
using Composable.SystemExtensions.Threading;

namespace AccountManagement
{
    public static class AccountManagementServerDomainBootstrapper
    {
        internal const string ConnectionStringName = "AccountManagement";

        public static IEndpoint RegisterWith(IEndpointHost host)
        {
            return host.RegisterAndStartEndpoint("UserManagement.Domain",
                                                 builder =>
                                                 {
                                                     SetupContainer(builder.Container);
                                                     RegisterHandlers(builder.RegisterHandlers);
                                                 });
        }

        public static void SetupContainer(IDependencyInjectionContainer container)
        {
            RegisterDomainComponents(container);

            container.RegisterSqlServerDocumentDb<
                IAccountManagementUiDocumentDbUpdater,
                IAccountManagementUiDocumentDbReader,
                IAccountManagementUiDocumentDbBulkReader>(ConnectionStringName);

            container.Register(
                Component.For<IAccountManagementQueryModelsReader>()
                         .UsingFactoryMethod((IAccountManagementUiDocumentDbReader documentDbQueryModels, AccountQueryModelGenerator accountQueryModelGenerator, ISingleContextUseGuard usageGuard) =>
                                                 new AccountManagementQueryModelReader(documentDbQueryModels, accountQueryModelGenerator, usageGuard))
                         .LifestyleScoped()
            );

            container.Register(Component.For<AccountQueryModelGenerator>()
                                        .ImplementedBy<AccountQueryModelGenerator>()
                                        .LifestyleScoped());

            container.Register(Component.For<EmailExistsQueryModelUpdater>()
                                        .ImplementedBy<EmailExistsQueryModelUpdater>()
                                        .LifestyleScoped(),
                               Component.For<UI.QueryModels.DocumentDB.Updaters.EmailToAccountMapQueryModelUpdater>()
                                        .ImplementedBy<UI.QueryModels.DocumentDB.Updaters.EmailToAccountMapQueryModelUpdater>()
                                        .LifestyleScoped());
        }


        static void RegisterDomainComponents(IDependencyInjectionContainer container)
        {
            container.RegisterSqlServerEventStore<
                IAccountManagementEventStoreUpdater,
                IAccountManagementEventStoreReader>(ConnectionStringName);

            container
                .RegisterSqlServerDocumentDb<
                    IAccountManagementDomainDocumentDbUpdater,
                    IAccountManagementDomainDocumentDbReader,
                    IAccountManagementDomainDocumentDbBulkReader>(ConnectionStringName);

            container.Register(
                Component.For<IAccountRepository>()
                         .UsingFactoryMethod((IAccountManagementEventStoreUpdater aggregates, IAccountManagementEventStoreReader reader) => new AccountRepository(aggregates, reader))
                         .LifestyleScoped(),
                Component.For<IDuplicateAccountChecker>()
                         .UsingFactoryMethod((IAccountManagementDomainDocumentDbReader queryModels) => new DuplicateAccountChecker(queryModels))
                         .LifestyleScoped()
            );
        }

        public static void RegisterHandlers(MessageHandlerRegistrarWithDependencyInjectionSupport registrar)
        {
            registrar.ForEvent((AccountEvent.PropertyUpdated.Email @event, UI.QueryModels.DocumentDB.Updaters.EmailToAccountMapQueryModelUpdater updater) => updater.Handle(@event));

            registrar.ForEvent((AccountEvent.PropertyUpdated.Email @event, EmailExistsQueryModelUpdater updater) => updater.Handle(@event));

            Account.MessageHandlers.RegisterHandlers(registrar);
        }
    }
}
