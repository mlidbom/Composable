using AccountManagement.Domain.Events.EventStore.ContainerInstallers;
using AccountManagement.Domain.Services;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Composable.Windsor.Persistence;
using JetBrains.Annotations;

namespace AccountManagement.Domain.ContainerInstallers
{
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
