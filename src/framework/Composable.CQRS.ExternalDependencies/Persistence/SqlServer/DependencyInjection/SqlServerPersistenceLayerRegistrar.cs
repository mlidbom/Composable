using Composable.DependencyInjection;
using Composable.Messaging.Buses;
using Composable.Messaging.Buses.Implementation;
using Composable.Persistence.SqlServer.Configuration;
using Composable.Persistence.SqlServer.Messaging.Buses.Implementation;
using Composable.Persistence.SqlServer.SystemExtensions;
using Composable.Refactoring.Naming;
using Composable.Serialization;
using Composable.System.Configuration;

namespace Composable.Persistence.SqlServer.DependencyInjection
{
    public static class SqlServerPersistenceLayerRegistrar
    {
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
        }
    }
}
