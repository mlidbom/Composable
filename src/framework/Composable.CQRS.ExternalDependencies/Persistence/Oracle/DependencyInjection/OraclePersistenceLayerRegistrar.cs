using Composable.DependencyInjection;
using Composable.Messaging.Buses;
using Composable.Messaging.Buses.Implementation;
using Composable.Persistence.DocumentDb;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.PersistenceLayer;
using Composable.Persistence.Oracle.DocumentDb;
using Composable.Persistence.Oracle.EventStore;
using Composable.Persistence.Oracle.Messaging.Buses.Implementation;
using Composable.Persistence.Oracle.SystemExtensions;
using Composable.Persistence.Oracle.Testing.Databases;
using Composable.Refactoring.Naming;
using Composable.SystemCE.ConfigurationCE;

namespace Composable.Persistence.Oracle.DependencyInjection
{
    public static class OraclePersistenceLayerRegistrar
    {
        public static void RegisterOraclePersistenceLayer(this IEndpointBuilder @this) =>
            @this.Container.RegisterOraclePersistenceLayer(@this.Configuration.ConnectionStringName);

        public static void RegisterOraclePersistenceLayer(this IDependencyInjectionContainer container, string connectionStringName)
        {
            //Connection management
            if(container.RunMode.IsTesting)
            {
                container.Register(Singleton.For<OracleDatabasePool>()
                                            .CreatedBy(((IConfigurationParameterProvider configurationParameterProvider) => new OracleDatabasePool()))
                                            .DelegateToParentServiceLocatorWhenCloning());

                container.Register(
                    Singleton.For<IOracleConnectionProvider>()
                             .CreatedBy((OracleDatabasePool pool) => new OracleConnectionProvider(() => pool.ConnectionStringFor(connectionStringName)))
                );
            } else
            {
                container.Register(
                    Singleton.For<IOracleConnectionProvider>()
                             .CreatedBy((IConfigurationParameterProvider configurationParameterProvider) => new OracleConnectionProvider(configurationParameterProvider.GetString(connectionStringName)))
                             .DelegateToParentServiceLocatorWhenCloning());
            }

            //Service bus
            container.Register(
                Singleton.For<IServiceBusPersistenceLayer.IOutboxPersistenceLayer>()
                         .CreatedBy((IOracleConnectionProvider endpointSqlConnection) => new OracleOutboxPersistenceLayer(endpointSqlConnection)),
                Singleton.For<IServiceBusPersistenceLayer.IInboxPersistenceLayer>()
                         .CreatedBy((IOracleConnectionProvider endpointSqlConnection) => new OracleInboxPersistenceLayer(endpointSqlConnection)));

            //DocumentDB
            container.Register(
                Singleton.For<IDocumentDbPersistenceLayer>()
                         .CreatedBy((IOracleConnectionProvider connectionProvider) => new OracleDocumentDbPersistenceLayer(connectionProvider)));


            //Event store
            container.Register(
                Singleton.For<OracleEventStoreConnectionManager>()
                         .CreatedBy((IOracleConnectionProvider sqlConnectionProvider) => new OracleEventStoreConnectionManager(sqlConnectionProvider)),
                Singleton.For<IEventStorePersistenceLayer>()
                         .CreatedBy((OracleEventStoreConnectionManager connectionManager, ITypeMapper typeMapper) => new OracleEventStorePersistenceLayer(connectionManager)));
        }
    }
}
