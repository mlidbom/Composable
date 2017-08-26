using AccountManagement.Domain.Services;
using Composable.DependencyInjection;
using Composable.DependencyInjection.Persistence;

namespace AccountManagement.Domain.ContainerInstallers
{
    static class AccountManagementDomainEventStoreInstaller
    {
        internal const string ConnectionStringName = "AccountManagement";

        internal static void SetupContainer(IDependencyInjectionContainer container)
        {
            container.RegisterSqlServerEventStore<
                IAccountManagementEventStoreUpdater,
                IAccountManagementEventStoreReader>(ConnectionStringName);
        }
    }
}
