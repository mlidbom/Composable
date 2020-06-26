using Composable.DependencyInjection;
using Composable.Messaging.Buses;
using Composable.Persistence.EventStore;
using Composable.Persistence.InMemory.EventStore;

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
            container.Register(Singleton.For<IEventStorePersistenceLayer>()
                                        .CreatedBy(()
                                                       => new InMemoryEventStorePersistenceLayer(
                                                           new InMemoryEventStorePersistenceLayerSchemaManager(),
                                                           new InMemoryEventStorePersistenceLayerReader(),
                                                           new InMemoryEventStorePersistenceLayerWriter())));
        }
    }
}
