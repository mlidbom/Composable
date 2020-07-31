using Composable.DependencyInjection;
using Composable.Messaging.Buses;
using Composable.Messaging.Buses.Implementation;
using Composable.Persistence.DocumentDb;
using Composable.Persistence.EventStore.PersistenceLayer;
using Composable.Persistence.DB2.DocumentDb;
using Composable.Persistence.DB2.EventStore;
using Composable.Persistence.DB2.Messaging.Buses.Implementation;
using Composable.Persistence.DB2.SystemExtensions;
using Composable.Persistence.DB2.Testing.Databases;
using Composable.Refactoring.Naming;
using Composable.SystemCE.ConfigurationCE;

namespace Composable.Persistence.DB2.DependencyInjection
{
    public static class DB2PersistenceLayerRegistrar
    {
        public static void RegisterDB2PersistenceLayer(this IEndpointBuilder @this) =>
            @this.Container.RegisterDB2PersistenceLayer(@this.Configuration.ConnectionStringName);

        public static void RegisterDB2PersistenceLayer(this IDependencyInjectionContainer container, string connectionStringName)
        {
            //Connection management
            if(container.RunMode.IsTesting)
            {
                container.Register(Singleton.For<DB2DatabasePool>()
                                            .CreatedBy(((IConfigurationParameterProvider configurationParameterProvider) => new DB2DatabasePool()))
                                            .DelegateToParentServiceLocatorWhenCloning());

                container.Register(
                    Singleton.For<IDB2ConnectionPool>()
                             .CreatedBy((DB2DatabasePool pool) => new DB2ConnectionPool(() => pool.ConnectionStringFor(connectionStringName)))
                );
            } else
            {
                container.Register(
                    Singleton.For<IDB2ConnectionPool>()
                             .CreatedBy((IConfigurationParameterProvider configurationParameterProvider) => DB2ConnectionPool.CreateInstance(configurationParameterProvider.GetString(connectionStringName)))
                             .DelegateToParentServiceLocatorWhenCloning());
            }

            //Service bus
            container.Register(
                Singleton.For<IServiceBusPersistenceLayer.IOutboxPersistenceLayer>()
                         .CreatedBy((IDB2ConnectionPool endpointSqlConnection) => new DB2OutboxPersistenceLayer(endpointSqlConnection)),
                Singleton.For<IServiceBusPersistenceLayer.IInboxPersistenceLayer>()
                         .CreatedBy((IDB2ConnectionPool endpointSqlConnection) => new DB2InboxPersistenceLayer(endpointSqlConnection)));

            //DocumentDB
            container.Register(
                Singleton.For<IDocumentDbPersistenceLayer>()
                         .CreatedBy((IDB2ConnectionPool connectionProvider) => new DB2DocumentDbPersistenceLayer(connectionProvider)));


            //Event store
            container.Register(
                Singleton.For<DB2EventStoreConnectionManager>()
                         .CreatedBy((IDB2ConnectionPool sqlConnectionProvider) => new DB2EventStoreConnectionManager(sqlConnectionProvider)),
                Singleton.For<IEventStorePersistenceLayer>()
                         .CreatedBy((DB2EventStoreConnectionManager connectionManager, ITypeMapper typeMapper) => new DB2EventStorePersistenceLayer(connectionManager)));
        }
    }
}
