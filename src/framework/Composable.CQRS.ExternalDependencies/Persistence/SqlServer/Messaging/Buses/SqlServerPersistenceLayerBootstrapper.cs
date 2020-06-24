using Composable.DependencyInjection;
using Composable.Messaging.Buses;
using Composable.Messaging.Buses.Implementation;
using Composable.Persistence.SqlServer.Configuration;
using Composable.Persistence.SqlServer.Messaging.Buses.Implementation;
using Composable.Persistence.SqlServer.SystemExtensions;
using Composable.Refactoring.Naming;
using Composable.Serialization;
using Composable.System.Configuration;

namespace Composable.Persistence.SqlServer.Messaging.Buses
{
    public static class SqlServerPersistenceLayerBootstrapper
    {
        public static void RegisterSqlServerPersistenceLayer(this IEndpointBuilder @this)
        {
            var endpointSqlConnection = new LazySqlServerConnectionProvider(
                () => @this.Container.CreateServiceLocator()
                           .Resolve<ISqlServerConnectionProviderSource>()
                           .GetConnectionProvider(@this.Configuration.ConnectionStringName).ConnectionString);

            @this.Container.Register(
                      Singleton.For<ISqlServerConnectionProviderSource>().CreatedBy(
                                    () => @this.Container.RunMode.IsTesting
                                              ? (ISqlServerConnectionProviderSource)new SqlServerServerDatabasePoolSqlServerConnectionProviderSource(@this.Container.CreateServiceLocator().Resolve<IConfigurationParameterProvider>())
                                              : new ConfigurationSqlServerConnectionProviderSource(@this.Container.CreateServiceLocator().Resolve<IConfigurationParameterProvider>())).DelegateToParentServiceLocatorWhenCloning(),
                      Singleton.For<InterprocessTransport.IMessageStorage>().CreatedBy(
                                    (ITypeMapper typeMapper, IRemotableMessageSerializer serializer)
                                        => new SqlServerInterProcessTransportMessageStorage(endpointSqlConnection, typeMapper,  serializer)),
                      Singleton.For<Inbox.IMessageStorage>().CreatedBy(() => new SqlServerMessageStorage(endpointSqlConnection))
                  );
        }
    }
}
