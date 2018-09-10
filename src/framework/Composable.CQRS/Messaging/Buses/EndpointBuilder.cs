using System;
using Composable.Contracts;
using Composable.DependencyInjection;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Buses.Implementation;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.Aggregates;
using Composable.Refactoring.Naming;
using Composable.Serialization;
using Composable.System;
using Composable.System.Configuration;
using Composable.System.Data.SqlClient;
using Composable.System.Threading;

// ReSharper disable ImplicitlyCapturedClosure it is very much intentional :)

namespace Composable.Messaging.Buses
{
    class EndpointBuilder : IEndpointBuilder
    {
        readonly IDependencyInjectionContainer _container;
        readonly TypeMapper _typeMapper;
        bool _builtSuccessfully;
        ISqlConnectionProviderSource _connectionProvider;

        public IDependencyInjectionContainer Container => _container;
        public ITypeMappingRegistar TypeMapper => _typeMapper;
        readonly IGlobalBusStateTracker _globalStateTracker;
        readonly MessageHandlerRegistry _registry;
        readonly LazySqlServerConnectionProvider _endpointSqlConnection;
        readonly IEndpointHost _host;
        public EndpointConfiguration Configuration { get; }

        public MessageHandlerRegistrarWithDependencyInjectionSupport RegisterHandlers { get; }

        public IEndpoint Build()
        {
            SetupContainer();
            SetupInternalTypeMap();
            BusApi.Internal.RegisterHandlers(RegisterHandlers);
            var endpoint = new Endpoint(_container.CreateServiceLocator(), Configuration);
            _builtSuccessfully = true;
            return endpoint;
        }

        void SetupInternalTypeMap()
        {
            EventStoreApi.MapTypes(TypeMapper);
            BusApi.MapTypes(TypeMapper);
        }

        public EndpointBuilder(IEndpointHost host, IGlobalBusStateTracker globalStateTracker, IDependencyInjectionContainer container, EndpointConfiguration configuration)
        {
            _host = host;
            _container = container;
            _globalStateTracker = globalStateTracker;


            Configuration = configuration;

            _endpointSqlConnection = new LazySqlServerConnectionProvider(
                () => _container.CreateServiceLocator().Resolve<ISqlConnectionProviderSource>().GetConnectionProvider(Configuration.ConnectionStringName).ConnectionString);

            _typeMapper = new TypeMapper(_endpointSqlConnection);

            _registry = new MessageHandlerRegistry(_typeMapper);
            RegisterHandlers = new MessageHandlerRegistrarWithDependencyInjectionSupport(_registry, new OptimizedLazy<IServiceLocator>(() => _container.CreateServiceLocator()));

        }

        void SetupContainer()
        {
            //todo: Find cleaner way of doing this.
            if(_host is IEndpointRegistry endpointRegistry)
            {
                _container.Register(Singleton.For<IEndpointRegistry>().Instance(endpointRegistry));
            } else
            {
                _container.Register(Singleton.For<IEndpointRegistry>().CreatedBy((IConfigurationParameterProvider configurationParameterProvider) => new AppConfigEndpointRegistry(configurationParameterProvider)));
            }

            if(!_container.HasComponent<IConfigurationParameterProvider>())
            {
                _container.Register(Singleton.For<IConfigurationParameterProvider>().CreatedBy(() => new AppConfigConfigurationParameterProvider()));
            }

            _container.Register(
                Singleton.For<ISqlConnectionProviderSource>().CreatedBy(() => _connectionProvider = _container.RunMode.IsTesting
                                                                                                                    ? (ISqlConnectionProviderSource)new SqlServerDatabasePoolSqlConnectionProviderSource(_container.CreateServiceLocator().Resolve<IConfigurationParameterProvider>())
                                                                                                                    : new ConfigurationSqlConnectionProviderSource(_container.CreateServiceLocator().Resolve<IConfigurationParameterProvider>())).DelegateToParentServiceLocatorWhenCloning(),
                Singleton.For<ITypeMappingRegistar, ITypeMapper, TypeMapper>().CreatedBy(() => _typeMapper).DelegateToParentServiceLocatorWhenCloning(),
                Singleton.For<ITaskRunner>().CreatedBy(() => new TaskRunner()),
                Singleton.For<EndpointId>().CreatedBy(() => Configuration.Id),
                Singleton.For<EndpointConfiguration>().CreatedBy(() => Configuration),
                Singleton.For<IInterprocessTransport>().CreatedBy((IUtcTimeTimeSource timeSource, ITaskRunner taskRunner, IRemotableMessageSerializer serializer) => new InterprocessTransport(_globalStateTracker, timeSource, _endpointSqlConnection, _typeMapper, Configuration, taskRunner, serializer)),
                Singleton.For<IGlobalBusStateTracker>().CreatedBy(() => _globalStateTracker),
                Singleton.For<IMessageHandlerRegistry, IMessageHandlerRegistrar, MessageHandlerRegistry>().CreatedBy(() => _registry),
                Singleton.For<IEventStoreSerializer>().CreatedBy(() => new EventStoreSerializer(_typeMapper)),
                Singleton.For<IDocumentDbSerializer>().CreatedBy(() => new DocumentDbSerializer(_typeMapper)),
                Singleton.For<IRemotableMessageSerializer>().CreatedBy(() => new RemotableMessageSerializer(_typeMapper)),
                Singleton.For<IAggregateTypeValidator>().CreatedBy(() => new AggregateTypeValidator(_typeMapper)),
                Singleton.For<IEventstoreEventPublisher>().CreatedBy((IInterprocessTransport interprocessTransport, IMessageHandlerRegistry messageHandlerRegistry) => new EventstoreEventPublisher(interprocessTransport, messageHandlerRegistry)),
                Scoped.For<IRemoteApiNavigatorSession>().CreatedBy((IInterprocessTransport interprocessTransport) => new RemoteApiBrowserSession(interprocessTransport)));

            if(Configuration.HasMessageHandlers)
            {
                _container.Register(
                    Singleton.For<IInbox>().CreatedBy((IServiceLocator serviceLocator, EndpointConfiguration endpointConfiguration, ITaskRunner taskRunner, IRemotableMessageSerializer serializer) => new Inbox(serviceLocator, _globalStateTracker, _registry, endpointConfiguration, _endpointSqlConnection, _typeMapper, taskRunner, serializer)),
                    Singleton.For<CommandScheduler>().CreatedBy((IInterprocessTransport transport, IUtcTimeTimeSource timeSource) => new CommandScheduler(transport, timeSource)),
                    Scoped.For<IServiceBusSession, ILocalApiNavigatorSession>().CreatedBy((IInterprocessTransport interprocessTransport, CommandScheduler commandScheduler, IMessageHandlerRegistry messageHandlerRegistry, IRemoteApiNavigatorSession remoteNavigator) => new ApiNavigatorSession(interprocessTransport, commandScheduler, messageHandlerRegistry, remoteNavigator))
                );
            }

            if(_container.RunMode == RunMode.Production)
            {
                _container.Register(Singleton.For<IUtcTimeTimeSource>().CreatedBy(() => new DateTimeNowTimeSource()).DelegateToParentServiceLocatorWhenCloning());
            } else
            {
                _container.Register(Singleton.For<IUtcTimeTimeSource, TestingTimeSource>().CreatedBy(() => TestingTimeSource.FollowingSystemClock).DelegateToParentServiceLocatorWhenCloning());
            }

            //Review:mlidbo: This is not pretty. Find a better way than a magic init method that has to be called at a magic moment.
            Configuration.Init(_container.CreateServiceLocator().Resolve<IConfigurationParameterProvider>());
        }

        bool _disposed;
        public void Dispose()
        {
            if(!_disposed)
            {
                _disposed = true;
                if(!_builtSuccessfully)
                {
                    (_connectionProvider as IDisposable)?.Dispose();
                    _container?.Dispose();
                }
            }
        }
    }
}
