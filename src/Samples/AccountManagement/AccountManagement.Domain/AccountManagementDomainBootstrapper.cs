using AccountManagement.Domain.ContainerInstallers;
using Composable.DependencyInjection;
using Composable.Messaging.Buses;

namespace AccountManagement.Domain
{
    public static class AccountManagementDomainBootstrapper
    {
        public static void SetupContainer(IDependencyInjectionContainer container)
        {

            AccountManagementDomainEventStoreInstaller.SetupContainer(container);
            AccountManagementDomainQuerymodelsSessionInstaller.SetupContainer(container);
            AccountRepositoryInstaller.SetupContainer(container);
            DuplicateAccountCheckerInstaller.SetupContainer(container);

            MessageHandlersInstaller.SetupContainer(container);


        }

        public static void RegisterHandlers(IMessageHandlerRegistrar registrar, IServiceLocator serviceLocator)
        {
            MessageHandlersInstaller.RegisterHandlers(registrar, serviceLocator);
        }
    }
}