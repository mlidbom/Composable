using Composable.DependencyInjection;

namespace AccountManagement.UI.QueryModels.DocumentDB.Updaters
{
    public static class AccountManagementUiQueryModelsDocumentDbUpdatersBootstrapper
    {
        public static void BootstrapForTesting(IDependencyInjectionContainer container)
        {
            ContainerInstallers.EventHandlersInstaller.Install(container);
        }
    }
}