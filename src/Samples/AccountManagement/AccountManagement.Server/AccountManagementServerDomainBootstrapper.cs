using System;
using AccountManagement.Domain;
using AccountManagement.UI;
using AccountManagement.UI.QueryModels;
using AccountManagement.UI.QueryModels.Services;
using AccountManagement.UI.QueryModels.Services.Implementation;
using Composable.DependencyInjection;
using Composable.DependencyInjection.Persistence;
using Composable.Messaging.Buses;
using Composable.Messaging.Buses.Implementation;

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
                                                     TypeMapper.MapTypes(builder.TypeMapper);
                                                     RegisterDomainComponents(builder.Container, builder.Configuration);
                                                     RegisterUserInterfaceComponents(builder.Container, builder.Configuration);

                                                     RegisterHandlers(builder.RegisterHandlers);
                                                 });
        }

        static void RegisterDomainComponents(IDependencyInjectionContainer container, EndpointConfiguration configuration)
        {
            container.RegisterSqlServerEventStore(configuration.ConnectionStringName);
            container.RegisterSqlServerDocumentDb(configuration.ConnectionStringName);
        }

        static void RegisterUserInterfaceComponents(IDependencyInjectionContainer container, EndpointConfiguration configuration)
        {
            container.RegisterSqlServerDocumentDb<IAccountManagementUiDocumentDbUpdater, IAccountManagementUiDocumentDbReader, IAccountManagementUiDocumentDbBulkReader>(configuration.ConnectionStringName);

            AccountManagementQueryModelReader.RegisterWith(container);
            AccountQueryModel.Generator.RegisterWith(container);
        }

        static void RegisterHandlers(MessageHandlerRegistrarWithDependencyInjectionSupport registrar)
        {
            AccountUIAdapter.GetById(registrar);
            AccountUIAdapter.Register(registrar);
            AccountUIAdapter.ChangeEmail(registrar);
            AccountUIAdapter.ChangePassword(registrar);
            AccountUIAdapter.Login(registrar);

            Account.Repository.Get(registrar);
            Account.Repository.Save(registrar);
            Account.Repository.GetReadonlyCopyOfLatestVersion(registrar);
            Account.Repository.GetReadonlyCopyOfVersion(registrar);

            EmailToAccountMapper.UpdateMappingWhenEmailChanges(registrar);
            EmailToAccountMapper.TryGetAccountByEmail(registrar);
        }
    }
}
