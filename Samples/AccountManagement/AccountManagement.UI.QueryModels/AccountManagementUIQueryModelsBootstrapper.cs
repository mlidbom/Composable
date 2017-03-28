using Composable.DependencyInjection;

namespace AccountManagement.UI.QueryModels
{
    public static class AccountManagementUiQueryModelsBootstrapper
    {
        public static void BootstrapForTesting(IDependencyInjectionContainer container)
        {
            ContainerInstallers.AccountManagementDocumentDbReaderInstaller.Install(container);
            ContainerInstallers.AccountManagementQueryModelReaderInstaller.Install(container);
            ContainerInstallers.QueryModelGeneratorsInstaller.Install(container);
        }
    }
}