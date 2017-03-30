using AccountManagement.Domain.Events.EventStore.ContainerInstallers;
using Composable.DependencyInjection;

namespace AccountManagement.Domain.Events.EventStore
{
    public static class AccountManagementDomainEventStoreBootstrapper
    {
        public static void BootstrapForTesting(IDependencyInjectionContainer container)
        {
            AccountManagementDomainEventStoreInstaller.Install(container);
        }
    }
}