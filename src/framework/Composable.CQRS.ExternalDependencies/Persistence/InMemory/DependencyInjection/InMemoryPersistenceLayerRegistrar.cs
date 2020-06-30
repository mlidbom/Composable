using Composable.DependencyInjection;
using Composable.Messaging.Buses;
using Composable.Messaging.Buses.Implementation;
using Composable.Persistence.DocumentDb;
using Composable.Persistence.EventStore;
using Composable.Persistence.InMemory.DocumentDB;
using Composable.Persistence.InMemory.EventStore;
using Composable.Persistence.InMemory.ServiceBus;
using Composable.Persistence.SqlServer.Messaging.Buses.Implementation;

namespace Composable.Persistence.InMemory.DependencyInjection
{
    public static class InMemoryPersistenceLayerRegistrar
    {
        //urgent: Register all InMemory persistence layer classes here.
        public static void RegisterInMemoryPersistenceLayer(this IEndpointBuilder @this)
        {
            @this.Container.RegisterInMemoryPersistenceLayer(@this.Configuration.ConnectionStringName);
        }

        public static void RegisterInMemoryPersistenceLayer(this IDependencyInjectionContainer container, string connectionStringName)
        {
            //DocumentDB
            container.Register(
                Singleton.For<IDocumentDbPersistenceLayer>()
                         .CreatedBy(() => new InMemoryDocumentDbPersistenceLayer()));


            //Service bus
            container.Register(
                Singleton.For<IServiceBusPersistenceLayer.IOutboxPersistenceLayer>()
                         .CreatedBy(() => new InMemoryOutboxPersistenceLayer()),
                Singleton.For<IServiceBusPersistenceLayer.IInboxPersistenceLayer>()
                         .CreatedBy(() => new InMemoryInboxPersistenceLayer()));

            container.Register(Singleton.For<IEventStorePersistenceLayer>()
                                        .CreatedBy(()
                                                       => new InMemoryEventStorePersistenceLayer(
                                                           new InMemoryEventStorePersistenceLayerSchemaManager(),
                                                           new InMemoryEventStorePersistenceLayerReader(),
                                                           new InMemoryEventStorePersistenceLayerWriter())));
        }
    }
}
