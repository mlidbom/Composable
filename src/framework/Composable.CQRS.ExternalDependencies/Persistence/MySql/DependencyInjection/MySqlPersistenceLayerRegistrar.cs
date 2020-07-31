using System;
using Composable.DependencyInjection;
using Composable.Messaging.Buses;
using Composable.Messaging.Buses.Implementation;
using Composable.Persistence.DocumentDb;
using Composable.Persistence.EventStore.PersistenceLayer;
using Composable.Persistence.MySql.DocumentDb;
using Composable.Persistence.MySql.EventStore;
using Composable.Persistence.MySql.Messaging.Buses.Implementation;
using Composable.Persistence.MySql.SystemExtensions;
using Composable.Persistence.MySql.Testing.Databases;
using Composable.Refactoring.Naming;
using Composable.SystemCE.ConfigurationCE;

namespace Composable.Persistence.MySql.DependencyInjection
{
    public static class MySqlPersistenceLayerRegistrar
    {
        public static void RegisterMySqlPersistenceLayer(this IEndpointBuilder @this) =>
            @this.Container.RegisterMySqlPersistenceLayer(@this.Configuration.ConnectionStringName);

        public static void RegisterMySqlPersistenceLayer(this IDependencyInjectionContainer container, string connectionStringName)
        {
            //Connection management
            if(container.RunMode.IsTesting)
            {
                container.Register(Singleton.For<MySqlDatabasePool>()
                                            .CreatedBy(((IConfigurationParameterProvider configurationParameterProvider) => new MySqlDatabasePool()))
                                            .DelegateToParentServiceLocatorWhenCloning());

                container.Register(
                    Singleton.For<IMySqlConnectionPool>()
                             .CreatedBy((MySqlDatabasePool pool) => IMySqlConnectionPool.CreateInstance(() => pool.ConnectionStringFor(connectionStringName)))
                );
            } else
            {
                container.Register(
                    Singleton.For<IMySqlConnectionPool>()
                             .CreatedBy((IConfigurationParameterProvider configurationParameterProvider) => IMySqlConnectionPool.CreateInstance(configurationParameterProvider.GetString(connectionStringName)))
                             .DelegateToParentServiceLocatorWhenCloning());
            }

            //Service bus
            container.Register(
                Singleton.For<IServiceBusPersistenceLayer.IOutboxPersistenceLayer>()
                         .CreatedBy((IMySqlConnectionPool endpointSqlConnection) => new MySqlOutboxPersistenceLayer(endpointSqlConnection)),
                Singleton.For<IServiceBusPersistenceLayer.IInboxPersistenceLayer>()
                         .CreatedBy((IMySqlConnectionPool endpointSqlConnection) => new MySqlInboxPersistenceLayer(endpointSqlConnection)));

            //DocumentDB
            container.Register(
                Singleton.For<IDocumentDbPersistenceLayer>()
                         .CreatedBy((IMySqlConnectionPool connectionProvider) => new MySqlDocumentDbPersistenceLayer(connectionProvider)));


            //Event store
            container.Register(
                Singleton.For<MySqlEventStoreConnectionManager>()
                         .CreatedBy((IMySqlConnectionPool sqlConnectionProvider) => new MySqlEventStoreConnectionManager(sqlConnectionProvider)),
                Singleton.For<IEventStorePersistenceLayer>()
                         .CreatedBy((MySqlEventStoreConnectionManager connectionManager, ITypeMapper typeMapper) => new MySqlEventStorePersistenceLayer(connectionManager)));
        }
    }
}
