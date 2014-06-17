using AccountManagement.Domain.Events.EventStore.Services;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.EventSourcing.SQLServer;
using Composable.System.Configuration;
using Composable.UnitsOfWork;
using JetBrains.Annotations;

namespace AccountManagement.Domain.Events.EventStore.ContainerInstallers
{
    [UsedImplicitly]
    public class AccountManagementDomainEventStoreInstaller : IWindsorInstaller
    {
        public static class ComponentKeys
        {
            public const string EventStore = "AccountManagement.Domain.EventStore";
            public const string EventStoreSession = "AccountManagement.Domain.EventStoreSession";
        }

        public const string ConnectionStringName = "AccountManagementDomain";

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(
                Component.For<IEventStore>()
                    .ImplementedBy<SqlServerEventStore>()
                    .DependsOn(new Dependency[] {Dependency.OnValue(typeof(string), GetConnectionStringFromConfiguration(ConnectionStringName))})
                    .Named(ComponentKeys.EventStore)
                    .LifestyleSingleton(),
                //Don't forget to register database components as IUnitOfWorkParticipant so that they work automatically with the framework management of units of work.
                Component.For<IAccountManagementEventStoreSession, IEventStoreReader, IAccountManagementEventStoreReader, IUnitOfWorkParticipant>()
                    .ImplementedBy<AccountManagementEventStoreSession>()
                    .DependsOn(new Dependency[] {Dependency.OnComponent(typeof(IEventStore), ComponentKeys.EventStore)})
                    .LifestylePerWebRequest()
                    .Named(ComponentKeys.EventStoreSession));
        }

        private static string GetConnectionStringFromConfiguration(string key)
        {
            return new ConnectionStringConfigurationParameterProvider().GetConnectionString(key).ConnectionString;
        }
    }
}
