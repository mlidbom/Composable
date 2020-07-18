using Composable.DependencyInjection;
using Composable.Messaging.Buses;
using Composable.Messaging.Buses.Implementation;
using Composable.Persistence.DocumentDb;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.PersistenceLayer;
using Composable.Persistence.InMemory.DocumentDB;
using Composable.Persistence.InMemory.EventStore;
using Composable.Persistence.InMemory.ServiceBus;

namespace Composable.Persistence.InMemory.DependencyInjection
{
    public static class InMemoryPersistenceLayerRegistrar
    {
        public static void RegisterInMemoryPersistenceLayer(this IEndpointBuilder @this)
        {
            @this.Container.RegisterInMemoryPersistenceLayer(@this.Configuration.ConnectionStringName);
        }

        public static void RegisterInMemoryPersistenceLayer(this IDependencyInjectionContainer container, string _)
        {
            //DocumentDB
            container.Register(
                Singleton.For<IDocumentDbPersistenceLayer>()
                         .CreatedBy(() => new InMemoryDocumentDbPersistenceLayer())
                         .DelegateToParentServiceLocatorWhenCloning());

            //Event store
            container.Register(Singleton.For<IEventStorePersistenceLayer>()
                                        .CreatedBy(() => new InMemoryEventStorePersistenceLayer())
                                        .DelegateToParentServiceLocatorWhenCloning());

            //Service bus
            container.Register(
                Singleton.For<IServiceBusPersistenceLayer.IOutboxPersistenceLayer>()
                         .CreatedBy(() => new InMemoryOutboxPersistenceLayer())
                         .DelegateToParentServiceLocatorWhenCloning(),
                Singleton.For<IServiceBusPersistenceLayer.IInboxPersistenceLayer>()
                         .CreatedBy(() => new InMemoryInboxPersistenceLayer())
                         .DelegateToParentServiceLocatorWhenCloning());
        }
    }
}
