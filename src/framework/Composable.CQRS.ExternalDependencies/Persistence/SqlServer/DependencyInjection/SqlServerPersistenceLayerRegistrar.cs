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
            public EventStorePersistenceLayer(IEventStoreSchemaManager schemaManager, IEventStoreEventReader eventReader, IEventStoreEventWriter eventWriter)
            {
                SchemaManager = schemaManager;
                EventReader = eventReader;
                EventWriter = eventWriter;
            }
            public IEventStoreSchemaManager SchemaManager { get; }
            public IEventStoreEventReader EventReader { get; }
            public IEventStoreEventWriter EventWriter { get; }
        }

        //urgent: Register all sql server persistence layer classes here.
        public static void RegisterSqlServerPersistenceLayer(this IEndpointBuilder @this)
        {
            var container = @this.Container;
            var configurationConnectionStringName = @this.Configuration.ConnectionStringName;

            RegisterSqlServerPersistenceLayer(container, configurationConnectionStringName);
        }

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

                Singleton.For<ISqlConnectionProvider>()
                         .CreatedBy((ISqlServerConnectionProviderSource providerSource) => new LazySqlServerConnectionProvider(() => providerSource.GetConnectionProvider(connectionStringName).ConnectionString)),
                Singleton.For<InterprocessTransport.IMessageStorage>().CreatedBy(
                              (ISqlConnectionProvider endpointSqlConnection, ITypeMapper typeMapper, IRemotableMessageSerializer serializer)
                                  => new SqlServerInterProcessTransportMessageStorage(endpointSqlConnection, typeMapper, serializer)),
                Singleton.For<Inbox.IMessageStorage>().CreatedBy((ISqlConnectionProvider endpointSqlConnection) => new SqlServerMessageStorage(endpointSqlConnection))
            );

            container.Register(
                Singleton.For<IEventStorePersistenceLayer>()
                         .CreatedBy((ISqlServerConnectionProviderSource connectionProviderSource, ITypeMapper typeMapper) =>
                          {
                              var connectionProvider = connectionProviderSource.GetConnectionProvider(connectionStringName);
                              var connectionManager = new SqlServerEventStoreConnectionManager(connectionProvider);
                              var schemaManager = new SqlServerEventStoreSchemaManager(connectionProvider);
                              var eventReader = new SqlServerEventStoreEventReader(connectionManager, typeMapper);
                              var eventWriter = new SqlServerEventStoreEventWriter(connectionManager);
                              return new EventStorePersistenceLayer(schemaManager, eventReader, eventWriter);
                          }));
        }
    }
}
