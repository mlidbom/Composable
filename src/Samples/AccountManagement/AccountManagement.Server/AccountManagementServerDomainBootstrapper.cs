using AccountManagement.ContainerInstallers;
using AccountManagement.UI.QueryModels;
using AccountManagement.UI.QueryModels.DocumentDB.Updaters;
using Composable.DependencyInjection;
using Composable.Messaging.Buses;

namespace AccountManagement
{
    public static class AccountManagementServerDomainBootstrapper
    {
        public static IEndpoint RegisterWith(IEndpointHost host)
        {
            return host.RegisterAndStartEndpoint("UserManagement.Domain", builder =>
            {
                SetupContainer(builder.Container);
                RegisterHandlers(builder.RegisterHandlers);
            });
        }

        public static void SetupContainer(IDependencyInjectionContainer container)
        {

            AccountManagementDomainEventStoreInstaller.SetupContainer(container);
            AccountManagementDomainQuerymodelsSessionInstaller.SetupContainer(container);
            AccountRepositoryInstaller.SetupContainer(container);
            DuplicateAccountCheckerInstaller.SetupContainer(container);


            AccountManagementUiQueryModelsBootstrapper.SetupContainer(container);

            MessageHandlersInstaller.SetupContainer(container);

            AccountManagementUiQueryModelsDocumentDbUpdatersBootstrapper.SetupContainer(container);



        }

        public static void RegisterHandlers(MessageHandlerRegistrarWithDependencyInjectionSupport registrar)
        {
            MessageHandlersInstaller.RegisterHandlers(registrar);
            ApiMessageHandlersInstaller.RegisterHandlers(registrar);
        }
    }
}