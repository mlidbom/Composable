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
        public static SqlServerEventStoreRegistration Registration = new SqlServerEventStoreRegistration<AccountManagementDomainEventStoreInstaller>();
        public const string ConnectionStringName = "AccountManagementDomain";

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.RegisterSqlServerEventStore(Registration, ConnectionStringName);

            container.Register(
                //Don't forget to register database components as IUnitOfWorkParticipant so that they work automatically with the framework management of units of work.
                Component.For<IAccountManagementEventStoreSession, IEventStoreReader, IAccountManagementEventStoreReader, IUnitOfWorkParticipant>()
                    .ImplementedBy<AccountManagementEventStoreSession>()
                    .DependsOn(Registration.Store)
                    .LifestylePerWebRequest());
        }

        private static string GetConnectionStringFromConfiguration(string key)
        {
            return new ConnectionStringConfigurationParameterProvider().GetConnectionString(key).ConnectionString;
        }
    }
}
