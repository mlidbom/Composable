using Composable.DependencyInjection;
using Composable.Messaging.Buses;
using Composable.Persistence.EventStore;
using Composable.Persistence.InMemory.EventStore;

namespace Composable.Persistence.InMemory.DependencyInjection
{
    public static class InMemoryPersistenceLayerRegistrar
    {
        //urgent: Register all MySql persistence layer classes here.
        public static void RegisterInMemoryPersistenceLayer(this IEndpointBuilder @this)
        {
            @this.Container.RegisterInMemoryPersistenceLayer(@this.Configuration.ConnectionStringName);
        }

        public static void RegisterInMemoryPersistenceLayer(this IDependencyInjectionContainer container, string connectionStringName)
        {
            container.Register(Singleton.For<IEventStorePersistenceLayer>()
                                        .CreatedBy((IEventTypeToIdMapper typeMapper)
                                                       => new InMemoryEventStorePersistenceLayer(
                                                           new InMemoryEventStoreSchemaManager(typeMapper),
                                                           new InMemoryEventStoreEventReader(),
                                                           new InMemoryEventStoreEventWriter())));
        }
    }
}
