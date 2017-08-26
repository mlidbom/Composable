using Composable.DependencyInjection;
using Composable.Messaging.Buses;

namespace AccountManagement.UI.QueryModels
{
    public static class AccountManagementUiQueryModelsBootstrapper
    {
        public static void SetupContainer(IDependencyInjectionContainer container)
        {
            ContainerInstallers.AccountManagementDocumentDbReaderInstaller.SetupContainer(container);
            ContainerInstallers.AccountManagementQueryModelReaderInstaller.SetupContainer(container);
            ContainerInstallers.QueryModelGeneratorsInstaller.SetupContainer(container);
        }

        public static void RegisterHandlers(IMessageHandlerRegistrar registrar, IServiceLocator serviceLocator)
        {
        }
    }
}