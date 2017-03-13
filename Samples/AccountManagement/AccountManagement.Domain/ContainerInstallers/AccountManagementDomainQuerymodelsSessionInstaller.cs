using AccountManagement.Domain.Events.EventStore.ContainerInstallers;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Composable.CQRS.CQRS.Windsor;
using Composable.Persistence.KeyValueStorage;
using JetBrains.Annotations;

namespace AccountManagement.Domain.ContainerInstallers
{
    // ReSharper disable MemberCanBeInternal unfortunately required by framework to be public at the moment for proxy generation to work.
    public interface IAccountManagementDomainDocumentDbSession : IDocumentDbSession {}

    public interface IAccountManagementDomainDocumentDbUpdater : IDocumentDbUpdater {}

    public interface IAccountManagementDomainDocumentDbReader : IDocumentDbReader {}

    public interface IAccountManagementDomainDocumentDbBulkReader : IDocumentDbBulkReader {}
    // ReSharper restore MemberCanBeInternal

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
