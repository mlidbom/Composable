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
        public static void RegisterSqlServerPersistenceLayer(this IEndpointBuilder @this)
        {
            var endpointSqlConnection = new LazySqlServerConnectionProvider(
                () => @this.Container.CreateServiceLocator()
                           .Resolve<ISqlConnectionProviderSource>()
                           .GetConnectionProvider(@this.Configuration.ConnectionStringName).ConnectionString);

            @this.Container.Register(
                Singleton.For<ISqlConnectionProviderSource>().CreatedBy(
                    () => @this.Container.RunMode.IsTesting
                              ? (ISqlConnectionProviderSource)new SqlServerDatabasePoolSqlConnectionProviderSource(@this.Container.CreateServiceLocator().Resolve<IConfigurationParameterProvider>())
                              : new ConfigurationSqlConnectionProviderSource(@this.Container.CreateServiceLocator().Resolve<IConfigurationParameterProvider>())).DelegateToParentServiceLocatorWhenCloning(),
                Singleton.For<InterprocessTransport.ISqlServerMessageStorage>().CreatedBy(
                    (ITypeMapper typeMapper, IRemotableMessageSerializer serializer)
                        => new SqlServerInterProcessTransportMessageStorage(endpointSqlConnection, typeMapper,  serializer)),
                Singleton.For<Inbox.IMessageStorage>().CreatedBy(() => new SqlServerMessageStorage(endpointSqlConnection))
            );
        }
    }
}
