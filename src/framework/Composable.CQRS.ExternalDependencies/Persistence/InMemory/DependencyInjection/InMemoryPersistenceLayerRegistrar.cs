using Composable.DependencyInjection;
using Composable.Messaging.Buses;
using Composable.Persistence.MySql.DependencyInjection;

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
            if(container.RunMode.IsTesting)
            {
            } else
            {
            }
        }
    }
}
