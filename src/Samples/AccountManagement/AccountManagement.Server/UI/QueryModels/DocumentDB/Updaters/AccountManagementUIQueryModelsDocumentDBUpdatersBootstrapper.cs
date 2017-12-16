using Composable.DependencyInjection;
using Composable.Messaging.Buses;

namespace AccountManagement.UI.QueryModels.DocumentDB.Updaters
{
    public static class AccountManagementUiQueryModelsDocumentDbUpdatersBootstrapper
    {
        public static void SetupContainer(IDependencyInjectionContainer container)
        {

            EventHandlersInstaller.Install(container);

            var serviceLocator = container.CreateServiceLocator();
            serviceLocator
                     .Use<IMessageHandlerRegistrar>(registrar =>
                                                    {
                                                        EventHandlersInstaller.Install(registrar, serviceLocator);
                                                    });
        }

        public static void RegisterHandlers(IMessageHandlerRegistrar registrar, IServiceLocator serviceLocator) {}
    }
}