using Composable.DependencyInjection;
using Composable.Messaging.Buses;

namespace Composable.Persistence.MySql.DependencyInjection
{
    public static class MySqlPersistenceLayerRegistrar
    {
        //urgent: Register all MySql persistence layer classes here.
        public static void RegisterMySqlPersistenceLayer(this IEndpointBuilder @this)
        {
           @this.Container.RegisterMySqlPersistenceLayer(@this.Configuration.ConnectionStringName);
        }

        public static void RegisterMySqlPersistenceLayer(this IDependencyInjectionContainer container, string connectionStringName)
        {
            if(container.RunMode.IsTesting)
            {
            } else
            {
            }
        }
    }
}
