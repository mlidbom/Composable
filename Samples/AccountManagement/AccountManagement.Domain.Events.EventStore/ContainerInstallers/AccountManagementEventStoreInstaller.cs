using AccountManagement.Domain.Events.EventStore.Services;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.EventSourcing.MicrosoftSQLServer;
using Composable.CQRS.Windsor;
using Composable.System.Configuration;
using Composable.UnitsOfWork;
using JetBrains.Annotations;

namespace AccountManagement.Domain.Events.EventStore.ContainerInstallers
{
    [UsedImplicitly]
    public class AccountManagementDomainEventStoreInstaller : IWindsorInstaller
    {
        public static SqlServerEventStoreRegistration Registration = new SqlServerEventStoreRegistration<AccountManagementEventStoreSession, IAccountManagementEventStoreSession, IAccountManagementEventStoreReader>();
        public const string ConnectionStringName = "AccountManagementDomain";

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.RegisterSqlServerEventStore(Registration, ConnectionStringName);
        }
    }
}
