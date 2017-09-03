using Composable.DependencyInjection;
using Composable.Messaging.Buses;

namespace AccountManagement.UI.QueryModels.DocumentDB.Updaters
{
    public static class AccountManagementUiQueryModelsDocumentDbUpdatersBootstrapper
    {
        public static void SetupContainer(IDependencyInjectionContainer container)
        {

            ContainerInstallers.EventHandlersInstaller.Install(container);

            var serviceLocator = container.CreateServiceLocator();
            serviceLocator
                     .Use<IMessageHandlerRegistrar>(registrar =>
                                                    {
                                                        ContainerInstallers.EventHandlersInstaller.Install(registrar, serviceLocator);
                                                    });
        }

        public static void RegisterHandlers(IMessageHandlerRegistrar registrar, IServiceLocator serviceLocator) {}
    }
}