using AccountManagement.Domain.Events.EventStore.Services;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Composable.CQRS.Windsor;
using JetBrains.Annotations;

namespace AccountManagement.Domain.Events.EventStore.ContainerInstallers
{
    [UsedImplicitly]
    public class AccountManagementDomainEventStoreInstaller : IWindsorInstaller
    {
        internal const string ConnectionStringName = "AccountManagementDomain";

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.RegisterSqlServerEventStore<
                IAccountManagementEventStoreSession,
                IAccountManagementEventStoreReader>(ConnectionStringName);
        }
    }
}
