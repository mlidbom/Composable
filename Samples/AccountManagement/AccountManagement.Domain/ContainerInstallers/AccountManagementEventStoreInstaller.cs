using System.Configuration;
using AccountManagement.Domain.Services;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.EventSourcing.SQLServer;
using Composable.CQRS.Windsor;
using Composable.CQRS.Windsor.Testing;
using Composable.System.Configuration;
using Composable.UnitsOfWork;

namespace AccountManagement.Domain.ContainerInstallers
{
    public class AccountManagementDomainEventStoreInstaller : IWindsorInstaller
    {
        public static class ComponentKeys
        {
            public const string EventStore = "AccountManagement.Domain.EventStore";
            public const string InMemoryEventStore = "AccountManagement.Domain.EventStore.InMemory";
            public const string EventStoreSession = "AccountManagement.Domain.EventStoreSession";
        }

        public static readonly string ConnectionStringName = "AccountManagementDomain";

        public void Install(IWindsorContainer container,IConfigurationStore store)
        {
            container.Register(
                Component.For<IEventStore, SqlServerEventStore>()
                    .ImplementedBy<SqlServerEventStore>()
                    .DependsOn(new Dependency[] { Dependency.OnValue(typeof(string), GetConnectionStringFromConfiguration(ConnectionStringName)) })
                    .Named(ComponentKeys.EventStore)
                    .LifestyleSingleton(),
                //Don't forget to register database components as IUnitOfWorkParticipant so that they work automatically with the framework management of units of work.
                Component.For<IAccountManagementEventStoreSession, IUnitOfWorkParticipant>()
                    .ImplementedBy<AccountManagementEventStoreSession>()
                    .DependsOn(new Dependency[] {Dependency.OnComponent(typeof(IEventStore), ComponentKeys.EventStore)})
                    .LifestylePerWebRequest()
                    .Named(ComponentKeys.EventStoreSession),
                //Provide a hook for tests to be able to use in-memory versions of the slow/hard to test database components.
                Component.For<IConfigureWiringForTests, IResetTestDatabases>()
                    .Instance(new DomainEventStoreTestConfigurator(container))
                );
        }

        private class DomainEventStoreTestConfigurator : IConfigureWiringForTests, IResetTestDatabases
        {
            private readonly IWindsorContainer _container;

            public DomainEventStoreTestConfigurator(IWindsorContainer container)
            {
                _container = container;
            }

            public void ConfigureWiringForTesting()
            {
                //Register an in memory version of the event store
                _container.Register(
                    Component.For<IEventStore, InMemoryEventStore>()
                        .ImplementedBy<InMemoryEventStore>()
                        .Named(ComponentKeys.InMemoryEventStore)
                        .LifestyleSingleton()
                        .IsDefault()
                    );

                //Make all clients use the in-memory version instead of the production version.
                _container.Kernel.AddHandlerSelector(
                    new KeyReplacementHandlerSelector(
                        typeof(IEventStore),
                        ComponentKeys.EventStore,
                        ComponentKeys.InMemoryEventStore));
            }

            public void ResetDatabase()
            {
                var eventStore = _container.Resolve<InMemoryEventStore>(ComponentKeys.InMemoryEventStore);
                eventStore.Reset();
                _container.Release(eventStore);
            }
        }

        private static string GetConnectionStringFromConfiguration(string key)
        {
            return new ConnectionStringConfigurationParameterProvider().GetConnectionString(key).ConnectionString;
        }
    }
}
