using AccountManagement.Domain.ContainerInstallers;
using Composable.DependencyInjection;
using Composable.Messaging.Buses;

namespace AccountManagement.Domain
{
    public static class AccountManagementDomainBootstrapper
    {
        public static void SetupForTesting(IDependencyInjectionContainer container)
        {
            container.CreateServiceLocator()
                     .Use<IMessageHandlerRegistrar>(registrar =>
                                                    {
                                                        AccountManagementDomainEventStoreInstaller.Install(container);
                                                        AccountManagementDomainQuerymodelsSessionInstaller.Install(container);
                                                        AccountRepositoryInstaller.Install(container);
                                                        DuplicateAccountCheckerInstaller.Install(container);
                                                        MessageHandlersInstaller.Install(registrar);
                                                    });
        }
    }
}