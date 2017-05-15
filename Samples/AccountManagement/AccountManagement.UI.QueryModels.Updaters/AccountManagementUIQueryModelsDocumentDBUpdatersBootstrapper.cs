using Composable.DependencyInjection;
using Composable.Messaging.Buses;

namespace AccountManagement.UI.QueryModels.DocumentDB.Updaters
{
    public static class AccountManagementUiQueryModelsDocumentDbUpdatersBootstrapper
    {
        public static void BootstrapForTesting(IDependencyInjectionContainer container)
        {
            container.CreateServiceLocator()
                     .Use<IMessageHandlerRegistrar>(registrar =>
                                                    {
                                                        ContainerInstallers.EventHandlersInstaller.Install(registrar);
                                                    });
        }
    }
}