using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.Windsor;
using Composable.Windsor;
using Composable.Windsor.Testing;
using JetBrains.Annotations;

namespace AccountManagement.Domain.Events.EventStore.ContainerInstallers.Testing
{
    [UsedImplicitly]
    public class InMemoryDomainEventStoreInstaller : IWindsorInstaller
    {
        public static class ComponentKeys
        {
            public const string InMemoryEventStore = "AccountManagement.Domain.EventStore.InMemory";
        }

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register( //Provide a hook for tests to be able to use in-memory versions of the slow/hard to test database components.
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
                        AccountManagementDomainEventStoreInstaller.ComponentKeys.EventStore,
                        ComponentKeys.InMemoryEventStore));
            }

            public void ResetDatabase()
            {
                var eventStore = _container.Resolve<InMemoryEventStore>(ComponentKeys.InMemoryEventStore);
                eventStore.Reset();
                _container.Release(eventStore);
            }
        }
    }
}
