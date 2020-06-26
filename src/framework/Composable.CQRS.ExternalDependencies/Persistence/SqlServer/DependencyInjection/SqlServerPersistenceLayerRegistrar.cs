using Composable.DependencyInjection;
using Composable.Messaging.Buses;
using Composable.Messaging.Buses.Implementation;
using Composable.Persistence.EventStore;
using Composable.Persistence.SqlServer.Configuration;
using Composable.Persistence.SqlServer.EventStore;
using Composable.Persistence.SqlServer.Messaging.Buses.Implementation;
using Composable.Persistence.SqlServer.SystemExtensions;
using Composable.Refactoring.Naming;
using Composable.Serialization;
using Composable.System.Configuration;

namespace Composable.Persistence.SqlServer.DependencyInjection
{
    public static class SqlServerPersistenceLayerRegistrar
    {
        class EventStorePersistenceLayer : IEventStorePersistenceLayer
        {
            public EventStorePersistenceLayer(IEventStorePersistenceLayer.ISchemaManager schemaManager, IEventStorePersistenceLayer.IReader eventReader, IEventStorePersistenceLayer.IWriter eventWriter)
            {
                SchemaManager = schemaManager;
                EventReader = eventReader;
                EventWriter = eventWriter;
            }
            public IEventStorePersistenceLayer.ISchemaManager SchemaManager { get; }
            public IEventStorePersistenceLayer.IReader EventReader { get; }
            public IEventStorePersistenceLayer.IWriter EventWriter { get; }
        }

        //urgent: Register all sql server persistence layer classes here.
        public static void RegisterSqlServerPersistenceLayer(this IEndpointBuilder @this)
        {
            var container = @this.Container;
            var configurationConnectionStringName = @this.Configuration.ConnectionStringName;

            RegisterSqlServerPersistenceLayer(container, configurationConnectionStringName);
        }

        //todo: does the fact that we register all this stuff using a connectionStringName mean that, using named components, we could easily have multiple registrations as long as they use different connectionStrings
        public static void RegisterSqlServerPersistenceLayer(this IDependencyInjectionContainer container, string connectionStringName)
        {
            if(container.RunMode.IsTesting)
            {
                container.Register(Singleton.For<ISqlServerConnectionProviderSource>()
                                            .CreatedBy((IConfigurationParameterProvider configurationParameterProvider)
                                                           => (ISqlServerConnectionProviderSource)new SqlServerServerDatabasePoolSqlServerConnectionProviderSource(configurationParameterProvider)).DelegateToParentServiceLocatorWhenCloning());
            } else
            {
                container.Register(Singleton.For<ISqlServerConnectionProviderSource>()
                                            .CreatedBy((IConfigurationParameterProvider configurationParameterProvider)
                                                           => new ConfigurationSqlServerConnectionProviderSource(configurationParameterProvider)).DelegateToParentServiceLocatorWhenCloning());
            }

            container.Register(
                Singleton.For<ISqlServerConnectionProvider>()
                         .CreatedBy((ISqlServerConnectionProviderSource providerSource) => new LazySqlServerConnectionProvider(() => providerSource.GetConnectionProvider(connectionStringName).ConnectionString))
            );

            //Service bus
            container.Register(Singleton.For<InterprocessTransport.IMessageStorage>()
                                        .CreatedBy((ISqlServerConnectionProvider endpointSqlConnection, ITypeMapper typeMapper, IRemotableMessageSerializer serializer)
                                                       => new SqlServerInterProcessTransportMessageStorage(endpointSqlConnection, typeMapper, serializer)),
                               Singleton.For<Inbox.IMessageStorage>().CreatedBy((ISqlServerConnectionProvider endpointSqlConnection) => new SqlServerMessageStorage(endpointSqlConnection)));

            //Event store
            container.Register(
                Singleton.For<SqlServerEventStoreConnectionManager>()
                         .CreatedBy((ISqlServerConnectionProvider sqlConnectionProvider) => new SqlServerEventStoreConnectionManager(sqlConnectionProvider)),
                Singleton.For<IEventStorePersistenceLayer.ISchemaManager>()
                         .CreatedBy((ISqlServerConnectionProvider sqlConnectionProvider) => new SqlServerEventStorePersistenceLayerSchemaManager(sqlConnectionProvider)),
                Singleton.For<IEventStorePersistenceLayer.IReader>()
                         .CreatedBy((SqlServerEventStoreConnectionManager connectionManager, ITypeMapper typeMapper) => new SqlServerEventStorePersistenceLayerReader(connectionManager, typeMapper)),
                Singleton.For<IEventStorePersistenceLayer.IWriter>()
                         .CreatedBy((SqlServerEventStoreConnectionManager connectionManager) => new SqlServerEventStorePersistenceLayerWriter(connectionManager)),
                Singleton.For<IEventStorePersistenceLayer>()
                         .CreatedBy((IEventStorePersistenceLayer.ISchemaManager schemaManager, IEventStorePersistenceLayer.IReader reader, IEventStorePersistenceLayer.IWriter writer) 
                                        => new EventStorePersistenceLayer(schemaManager, reader, writer)));
        }
    }
}
