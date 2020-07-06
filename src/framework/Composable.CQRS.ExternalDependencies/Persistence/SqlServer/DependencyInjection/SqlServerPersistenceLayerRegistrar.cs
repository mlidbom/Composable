using Composable.DependencyInjection;
using Composable.Messaging.Buses;
using Composable.Messaging.Buses.Implementation;
using Composable.Persistence.DocumentDb;
using Composable.Persistence.EventStore;
using Composable.Persistence.SqlServer.DocumentDb;
using Composable.Persistence.SqlServer.EventStore;
using Composable.Persistence.SqlServer.Messaging.Buses.Implementation;
using Composable.Persistence.SqlServer.SystemExtensions;
using Composable.Persistence.SqlServer.Testing.Databases;
using Composable.Refactoring.Naming;
using Composable.System.Configuration;

namespace Composable.Persistence.SqlServer.DependencyInjection
{
    public static class SqlServerPersistenceLayerRegistrar
    {
       public static void RegisterSqlServerPersistenceLayer(this IEndpointBuilder @this)
        {
            var container = @this.Container;
            var configurationConnectionStringName = @this.Configuration.ConnectionStringName;

            RegisterSqlServerPersistenceLayer(container, configurationConnectionStringName);
        }

        //todo: does the fact that we register all this stuff using a connectionStringName mean that, using named components, we could easily have multiple registrations as long as they use different connectionStrings
        public static void RegisterSqlServerPersistenceLayer(this IDependencyInjectionContainer container, string connectionStringName)
        {
            //Connection management
            if(container.RunMode.IsTesting)
            {
                container.Register(Singleton.For<SqlServerDatabasePool>()
                                            .CreatedBy(((IConfigurationParameterProvider configurationParameterProvider) => new SqlServerDatabasePool()))
                                            .DelegateToParentServiceLocatorWhenCloning());

                container.Register(
                    Singleton.For<ISqlServerConnectionProvider>()
                             .CreatedBy((SqlServerDatabasePool pool) => new SqlServerConnectionProvider(pool.ConnectionStringFor(connectionStringName)))
                );
            } else
            {
                container.Register(
                    Singleton.For<ISqlServerConnectionProvider>()
                             .CreatedBy((IConfigurationParameterProvider configurationParameterProvider) => new SqlServerConnectionProvider(configurationParameterProvider.GetString(connectionStringName)))
                             .DelegateToParentServiceLocatorWhenCloning());
            }


            //Service bus
            //Bug: Urgent: Registering these as Scoped does not cause a failure even though the endpoint builder wires singletons to use them. Strangely, doing the same with the in-memory version does cause a failure!
            container.Register(
                Singleton.For<IServiceBusPersistenceLayer.IOutboxPersistenceLayer>()
                         .CreatedBy((ISqlServerConnectionProvider endpointSqlConnection) => new SqlServerOutboxPersistenceLayer(endpointSqlConnection)),
                Singleton.For<IServiceBusPersistenceLayer.IInboxPersistenceLayer>()
                         .CreatedBy((ISqlServerConnectionProvider endpointSqlConnection) => new SqlServerInboxPersistenceLayer(endpointSqlConnection)));

            //DocumentDB
            container.Register(
                Singleton.For<IDocumentDbPersistenceLayer>()
                         .CreatedBy((ISqlServerConnectionProvider connectionProvider) => new SqlServerDocumentDbPersistenceLayer(connectionProvider)));

            //Event store
            container.Register(
                Singleton.For<SqlServerEventStoreConnectionManager>()
                         .CreatedBy((ISqlServerConnectionProvider sqlConnectionProvider) => new SqlServerEventStoreConnectionManager(sqlConnectionProvider)),
                Singleton.For<IEventStorePersistenceLayer>()
                         .CreatedBy((SqlServerEventStoreConnectionManager connectionManager, ITypeMapper typeMapper) => new SqlServerEventStorePersistenceLayer(connectionManager)));
        }
    }
}
