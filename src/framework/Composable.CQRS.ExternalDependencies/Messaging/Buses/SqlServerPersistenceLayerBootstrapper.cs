using Composable.DependencyInjection;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Buses.Implementation;
using Composable.Refactoring.Naming;
using Composable.Serialization;
using Composable.System.Configuration;
using Composable.System.Data.SqlClient;
using Composable.System.Threading;

namespace Composable.Messaging.Buses
{
    public static class SqlServerPersistenceLayerBootstrapper
    {
        public static IDependencyInjectionContainer RegisterSqlServerPersistenceLayer(this IDependencyInjectionContainer @this, EndpointConfiguration endpointConfiguration)
        {
            var endpointSqlConnection = new LazySqlServerConnectionProvider(
                () => @this.CreateServiceLocator()
                           .Resolve<ISqlConnectionProviderSource>()
                           .GetConnectionProvider(endpointConfiguration.ConnectionStringName).ConnectionString);

            @this.Register(
                Singleton.For<ISqlConnectionProviderSource>().CreatedBy(
                    () => @this.RunMode.IsTesting
                              ? (ISqlConnectionProviderSource)new SqlServerDatabasePoolSqlConnectionProviderSource(@this.CreateServiceLocator().Resolve<IConfigurationParameterProvider>())
                              : new ConfigurationSqlConnectionProviderSource(@this.CreateServiceLocator().Resolve<IConfigurationParameterProvider>())).DelegateToParentServiceLocatorWhenCloning(),
                Singleton.For<InterprocessTransport.ISqlServerMessageStorage>().CreatedBy(
                    (ITypeMapper typeMapper, IRemotableMessageSerializer serializer)
                        => new SqlServerInterProcessTransportMessageStorage(endpointSqlConnection, typeMapper,  serializer)),
                Singleton.For<Inbox.IMessageStorage>().CreatedBy(() => new SqlServerMessageStorage(endpointSqlConnection))
            );

            return @this;
        }
    }
}
