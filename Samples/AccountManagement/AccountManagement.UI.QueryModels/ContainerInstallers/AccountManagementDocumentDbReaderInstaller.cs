using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Composable.CQRS.CQRS.Windsor;
using Composable.Persistence.KeyValueStorage;
using JetBrains.Annotations;

namespace AccountManagement.UI.QueryModels.ContainerInstallers
{
    // ReSharper disable MemberCanBeInternal unfortunately required by framework to be public at the moment for proxy generation to work.
    public interface IAccountManagementUiDocumentDbSession : IDocumentDbSession { }

    public interface IAccountManagementUiDocumentDbUpdater : IDocumentDbUpdater { }

    public interface IAccountManagementUiDocumentDbReader : IDocumentDbReader { }

    public interface IAccountManagementUiDocumentDbBulkReader : IDocumentDbBulkReader { }
    // ReSharper restore MemberCanBeInternal

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
