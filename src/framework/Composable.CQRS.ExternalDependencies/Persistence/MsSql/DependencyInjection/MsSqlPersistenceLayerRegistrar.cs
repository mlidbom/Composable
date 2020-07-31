using Composable.DependencyInjection;
using Composable.Messaging.Buses;
using Composable.Messaging.Buses.Implementation;
using Composable.Persistence.DocumentDb;
using Composable.Persistence.EventStore.PersistenceLayer;
using Composable.Persistence.MsSql.DocumentDb;
using Composable.Persistence.MsSql.EventStore;
using Composable.Persistence.MsSql.Messaging.Buses.Implementation;
using Composable.Persistence.MsSql.SystemExtensions;
using Composable.Persistence.MsSql.Testing.Databases;
using Composable.Refactoring.Naming;
using Composable.SystemCE.ConfigurationCE;

namespace Composable.Persistence.MsSql.DependencyInjection
{
    public static class MsSqlPersistenceLayerRegistrar
    {
       public static void RegisterMsSqlPersistenceLayer(this IEndpointBuilder @this) =>
           @this.Container.RegisterMsSqlPersistenceLayer(@this.Configuration.ConnectionStringName);

       //todo: does the fact that we register all this stuff using a connectionStringName mean that, using named components, we could easily have multiple registrations as long as they use different connectionStrings
        public static void RegisterMsSqlPersistenceLayer(this IDependencyInjectionContainer container, string connectionStringName)
        {
            //Connection management
            if(container.RunMode.IsTesting)
            {
                container.Register(Singleton.For<MsSqlDatabasePool>()
                                            .CreatedBy(((IConfigurationParameterProvider configurationParameterProvider) => new MsSqlDatabasePool()))
                                            .DelegateToParentServiceLocatorWhenCloning());

                container.Register(
                    Singleton.For<IMsSqlConnectionPool>()
                             .CreatedBy((MsSqlDatabasePool pool) => new MsSqlConnectionPool(() => pool.ConnectionStringFor(connectionStringName)))
                );
            } else
            {
                container.Register(
                    Singleton.For<IMsSqlConnectionPool>()
                             .CreatedBy((IConfigurationParameterProvider configurationParameterProvider) => MsSqlConnectionPool.CreateInstance(configurationParameterProvider.GetString(connectionStringName)))
                             .DelegateToParentServiceLocatorWhenCloning());
            }


            //Service bus
            container.Register(
                Singleton.For<IServiceBusPersistenceLayer.IOutboxPersistenceLayer>()
                         .CreatedBy((IMsSqlConnectionPool endpointSqlConnection) => new MsSqlOutboxPersistenceLayer(endpointSqlConnection)),
                Singleton.For<IServiceBusPersistenceLayer.IInboxPersistenceLayer>()
                         .CreatedBy((IMsSqlConnectionPool endpointSqlConnection) => new MsSqlInboxPersistenceLayer(endpointSqlConnection)));

            //DocumentDB
            container.Register(
                Singleton.For<IDocumentDbPersistenceLayer>()
                         .CreatedBy((IMsSqlConnectionPool connectionProvider) => new MsSqlDocumentDbPersistenceLayer(connectionProvider)));

            //Event store
            container.Register(
                Singleton.For<MsSqlEventStoreConnectionManager>()
                         .CreatedBy((IMsSqlConnectionPool sqlConnectionProvider) => new MsSqlEventStoreConnectionManager(sqlConnectionProvider)),
                Singleton.For<IEventStorePersistenceLayer>()
                         .CreatedBy((MsSqlEventStoreConnectionManager connectionManager, ITypeMapper typeMapper) => new MsSqlEventStorePersistenceLayer(connectionManager)));
        }
    }
}
