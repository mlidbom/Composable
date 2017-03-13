using AccountManagement.Domain.Events.EventStore.ContainerInstallers;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Composable.CQRS.CQRS.Windsor;
using Composable.Persistence.KeyValueStorage;
using JetBrains.Annotations;

namespace AccountManagement.Domain.ContainerInstallers
{
    interface IAccountManagementDomainDocumentDbSession : IDocumentDbSession {}

    interface IAccountManagementDomainDocumentDbUpdater : IDocumentDbUpdater {}

    interface IAccountManagementDomainDocumentDbReader : IDocumentDbReader {}

    interface IAccountManagementDomainDocumentDbBulkReader : IDocumentDbBulkReader {}

    [UsedImplicitly] public class AccountManagementDomainQuerymodelsSessionInstaller : IWindsorInstaller
    {
        public void Install(
            IWindsorContainer container,
            IConfigurationStore store)
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
