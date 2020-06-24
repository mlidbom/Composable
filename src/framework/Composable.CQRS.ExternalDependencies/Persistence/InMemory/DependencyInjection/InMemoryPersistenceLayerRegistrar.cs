using Composable.Messaging.Buses;

namespace Composable.Persistence.InMemory.DependencyInjection
{
    public static class InMemoryPersistenceLayerRegistrar
    {
        //urgent: Register all InMemory persistence layer classes here.
        public static void RegisterInMemoryPersistenceLayer(this IEndpointBuilder @this)
        {
           @this.Container.Register();
        }
    }
}
