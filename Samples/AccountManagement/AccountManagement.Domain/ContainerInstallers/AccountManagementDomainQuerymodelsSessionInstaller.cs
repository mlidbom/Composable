using AccountManagement.Domain.Events.EventStore.ContainerInstallers;
using AccountManagement.Domain.Services;
using Composable.DependencyInjection;
using Composable.Windsor.Persistence;

namespace AccountManagement.Domain.ContainerInstallers
{
    static class AccountManagementDomainQuerymodelsSessionInstaller
    {
        internal static void Install(IDependencyInjectionContainer container)
        {
            container
                .RegisterSqlServerDocumentDb<
                    IAccountManagementDomainDocumentDbSession,
                    IAccountManagementDomainDocumentDbUpdater,
                    IAccountManagementDomainDocumentDbReader,
                    IAccountManagementDomainDocumentDbBulkReader>(AccountManagementDomainEventStoreInstaller
                                                                .ConnectionStringName);
        }
    }
}
