using Composable.DependencyInjection;
using Composable.Messaging.Buses;
using Composable.Messaging.Buses.Implementation;
using Composable.Persistence.DocumentDb;
using Composable.Persistence.EventStore;
using Composable.Persistence.MySql.DocumentDb;
using Composable.Persistence.MySql.EventStore;
using Composable.Persistence.MySql.Messaging.Buses.Implementation;
using Composable.Persistence.MySql.SystemExtensions;
using Composable.Persistence.MySql.Testing.Databases;
using Composable.Refactoring.Naming;
using Composable.System.Configuration;

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
            //Connection management
            if(container.RunMode.IsTesting)
            {
                container.Register(Singleton.For<MySqlDatabasePool>()
                                            .CreatedBy(((IConfigurationParameterProvider configurationParameterProvider) => new MySqlDatabasePool()))
                                            .DelegateToParentServiceLocatorWhenCloning());

                container.Register(
                    Singleton.For<IMySqlConnectionProvider>()
                             .CreatedBy((MySqlDatabasePool pool) => new MySqlConnectionProvider(pool.ConnectionStringFor(connectionStringName)))
                );
            } else
            {
                container.Register(
                    Singleton.For<IMySqlConnectionProvider>()
                             .CreatedBy((IConfigurationParameterProvider configurationParameterProvider) => new MySqlConnectionProvider(configurationParameterProvider.GetString(connectionStringName)))
                             .DelegateToParentServiceLocatorWhenCloning());
            }

            //Service bus
            container.Register(
                Singleton.For<IServiceBusPersistenceLayer.IOutboxPersistenceLayer>()
                         .CreatedBy((IMySqlConnectionProvider endpointSqlConnection) => new MySqlOutboxPersistenceLayer(endpointSqlConnection)),
                Singleton.For<IServiceBusPersistenceLayer.IInboxPersistenceLayer>()
                         .CreatedBy((IMySqlConnectionProvider endpointSqlConnection) => new MySqlInboxPersistenceLayer(endpointSqlConnection)));

            //DocumentDB
            container.Register(
                Singleton.For<IDocumentDbPersistenceLayer>()
                         .CreatedBy((IMySqlConnectionProvider connectionProvider) => new MySqlDocumentDbPersistenceLayer(connectionProvider)));


            //Event store
            container.Register(
                Singleton.For<MySqlEventStoreConnectionManager>()
                         .CreatedBy((IMySqlConnectionProvider sqlConnectionProvider) => new MySqlEventStoreConnectionManager(sqlConnectionProvider)),
                Singleton.For<IEventStorePersistenceLayer>()
                         .CreatedBy((MySqlEventStoreConnectionManager connectionManager, ITypeMapper typeMapper) => new MySqlEventStorePersistenceLayer(connectionManager)));
        }
    }
}
