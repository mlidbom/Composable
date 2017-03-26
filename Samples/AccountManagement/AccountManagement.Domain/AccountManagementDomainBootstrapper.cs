using AccountManagement.Domain.ContainerInstallers;
using Composable.DependencyInjection;

namespace AccountManagement.Domain
{
    public static class AccountManagementDomainBootstrapper
    {
        public static void SetupForTesting(IDependencyInjectionContainer container)
        {
            AccountManagementDomainQuerymodelsSessionInstaller.Install(container);
            AccountRepositoryInstaller.Install(container);
            DuplicateAccountCheckerInstaller.Install(container);
            MessageHandlersInstaller.Install(container);
        }
    }
}