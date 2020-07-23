using Composable.DependencyInjection;
using Composable.Messaging.Buses;
using Composable.Messaging.Buses.Implementation;
using Composable.Persistence.DocumentDb;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.PersistenceLayer;
using Composable.Persistence.PgSql.DocumentDb;
using Composable.Persistence.PgSql.EventStore;
using Composable.Persistence.PgSql.Messaging.Buses.Implementation;
using Composable.Persistence.PgSql.SystemExtensions;
using Composable.Persistence.PgSql.Testing.Databases;
using Composable.Refactoring.Naming;
using Composable.SystemCE.Configuration;

namespace Composable.Persistence.PgSql.DependencyInjection
{
    public static class PgSqlPersistenceLayerRegistrar
    {
        public static void RegisterPgSqlPersistenceLayer(this IEndpointBuilder @this) =>
            @this.Container.RegisterPgSqlPersistenceLayer(@this.Configuration.ConnectionStringName);

        public static void RegisterPgSqlPersistenceLayer(this IDependencyInjectionContainer container, string connectionStringName)
        {
            //Connection management
            if(container.RunMode.IsTesting)
            {
                container.Register(Singleton.For<PgSqlDatabasePool>()
                                            .CreatedBy(((IConfigurationParameterProvider configurationParameterProvider) => new PgSqlDatabasePool()))
                                            .DelegateToParentServiceLocatorWhenCloning());

                container.Register(
                    Singleton.For<INpgsqlConnectionProvider>()
                             .CreatedBy((PgSqlDatabasePool pool) => new PgSqlConnectionProvider(() => pool.ConnectionStringFor(connectionStringName)))
                );
            } else
            {
                container.Register(
                    Singleton.For<INpgsqlConnectionProvider>()
                             .CreatedBy((IConfigurationParameterProvider configurationParameterProvider) => new PgSqlConnectionProvider(configurationParameterProvider.GetString(connectionStringName)))
                             .DelegateToParentServiceLocatorWhenCloning());
            }

            //Service bus
            container.Register(
                Singleton.For<IServiceBusPersistenceLayer.IOutboxPersistenceLayer>()
                         .CreatedBy((INpgsqlConnectionProvider endpointSqlConnection) => new PgSqlOutboxPersistenceLayer(endpointSqlConnection)),
                Singleton.For<IServiceBusPersistenceLayer.IInboxPersistenceLayer>()
                         .CreatedBy((INpgsqlConnectionProvider endpointSqlConnection) => new PgSqlInboxPersistenceLayer(endpointSqlConnection)));

            //DocumentDB
            container.Register(
                Singleton.For<IDocumentDbPersistenceLayer>()
                         .CreatedBy((INpgsqlConnectionProvider connectionProvider) => new PgSqlDocumentDbPersistenceLayer(connectionProvider)));


            //Event store
            container.Register(
                Singleton.For<PgSqlEventStoreConnectionManager>()
                         .CreatedBy((INpgsqlConnectionProvider sqlConnectionProvider) => new PgSqlEventStoreConnectionManager(sqlConnectionProvider)),
                Singleton.For<IEventStorePersistenceLayer>()
                         .CreatedBy((PgSqlEventStoreConnectionManager connectionManager, ITypeMapper typeMapper) => new PgSqlEventStorePersistenceLayer(connectionManager)));
        }
    }
}
