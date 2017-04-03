using AccountManagement.Domain.Services;
using Composable.DependencyInjection;
using Composable.DependencyInjection.Persistence;

namespace AccountManagement.Domain.ContainerInstallers
{
    static class AccountManagementDomainEventStoreInstaller
    {
        internal const string ConnectionStringName = "AccountManagementDomain";

        internal static void Install(IDependencyInjectionContainer container)
        {
            container.RegisterSqlServerEventStore<
                IAccountManagementEventStoreUpdater,
                IAccountManagementEventStoreReader>(ConnectionStringName);
        }
    }
}
