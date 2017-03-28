using AccountManagement.UI.QueryModels.Services;
using Composable.DependencyInjection;
using Composable.DependencyInjection.Persistence;

namespace AccountManagement.UI.QueryModels.ContainerInstallers
{
    static class AccountManagementDocumentDbReaderInstaller
    {
        const string ConnectionStringName = "AccountManagementReadModels";

        internal static void Install(IDependencyInjectionContainer container)
        {
            container.RegisterSqlServerDocumentDb<
                         IAccountManagementUiDocumentDbSession,
                         IAccountManagementUiDocumentDbUpdater,
                         IAccountManagementUiDocumentDbReader,
                         IAccountManagementUiDocumentDbBulkReader>(ConnectionStringName);
        }
    }
}
