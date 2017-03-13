using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Composable.CQRS.CQRS.Windsor;
using Composable.Persistence.KeyValueStorage;
using JetBrains.Annotations;

namespace AccountManagement.UI.QueryModels.ContainerInstallers
{
    interface IAccountManagementUiDocumentDbSession : IDocumentDbSession { }

    interface IAccountManagementUiDocumentDbUpdater : IDocumentDbUpdater { }

    interface IAccountManagementUiDocumentDbReader : IDocumentDbReader { }

    interface IAccountManagementUiDocumentDbBulkReader : IDocumentDbBulkReader { }

    [UsedImplicitly]
    public class AccountManagementDocumentDbReaderInstaller : IWindsorInstaller
    {
        const string ConnectionStringName = "AccountManagementReadModels";

        public void Install(
            IWindsorContainer container,
            IConfigurationStore store)
        {
            container.RegisterSqlServerDocumentDb<
                         IAccountManagementUiDocumentDbSession,
                         IAccountManagementUiDocumentDbUpdater,
                         IAccountManagementUiDocumentDbReader,
                         IAccountManagementUiDocumentDbBulkReader>(ConnectionStringName);
        }
    }
}
