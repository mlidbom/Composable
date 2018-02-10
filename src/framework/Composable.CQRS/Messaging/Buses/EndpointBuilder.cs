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

        public IDependencyInjectionContainer Container => _container;
        public ITypeMappingRegistar TypeMapper => _typeMapper;
        public EndpointConfiguration Configuration { get; }

        public MessageHandlerRegistrarWithDependencyInjectionSupport RegisterHandlers { get; }

        public IEndpoint Build()
        {
            SetupInternalTypeMap();
            BusApi.Internal.RegisterHandlers(RegisterHandlers);
            return new Endpoint(_container.CreateServiceLocator(), Configuration);
        }

        void SetupInternalTypeMap()
        {
            EventStoreApi.MapTypes(TypeMapper);
            BusApi.MapTypes(TypeMapper);
        }

        public EndpointBuilder(IGlobalBusStateTracker globalStateTracker, IDependencyInjectionContainer container, EndpointConfiguration configuration)
        {
            _container = container;

            Configuration = configuration;

            var connectionProvider = container.RunMode.IsTesting
                                         ? (ISqlConnectionProvider)new SqlServerDatabasePoolSqlConnectionProvider()
                                         : new AppConfigSqlConnectionProvider();

            var endpointSqlConnection = new LazySqlServerConnection(new OptimizedLazy<string>(() => connectionProvider.GetConnectionProvider(Configuration.ConnectionStringName).ConnectionString));

            _typeMapper = new TypeMapper(endpointSqlConnection);

            var registry = new MessageHandlerRegistry(_typeMapper);
            RegisterHandlers = new MessageHandlerRegistrarWithDependencyInjectionSupport(registry, new OptimizedLazy<IServiceLocator>(() => _container.CreateServiceLocator()));

            _container.Register(
                Singleton.For<ISqlConnectionProvider>().UsingFactoryMethod(() => connectionProvider).DelegateToParentServiceLocatorWhenCloning(),
                Singleton.For<ITypeMappingRegistar, ITypeMapper, TypeMapper>().UsingFactoryMethod(() => _typeMapper).DelegateToParentServiceLocatorWhenCloning(),
                Singleton.For<ITaskRunner>().UsingFactoryMethod(() => new TaskRunner()),
                Singleton.For<EndpointId>().UsingFactoryMethod(() => configuration.Id),
                Singleton.For<EndpointConfiguration>().UsingFactoryMethod(() => Configuration),
                Singleton.For<IInterprocessTransport>().UsingFactoryMethod((IUtcTimeTimeSource timeSource, ITaskRunner taskRunner, IRemotableMessageSerializer serializer) => new InterprocessTransport(globalStateTracker, timeSource, endpointSqlConnection, _typeMapper, configuration, taskRunner, serializer)),
                Singleton.For<IGlobalBusStateTracker>().UsingFactoryMethod(() => globalStateTracker),
                Singleton.For<IMessageHandlerRegistry, IMessageHandlerRegistrar, MessageHandlerRegistry>().UsingFactoryMethod(() => registry),
                Singleton.For<IEventStoreSerializer>().UsingFactoryMethod(() => new EventStoreSerializer(_typeMapper)),
                Singleton.For<IDocumentDbSerializer>().UsingFactoryMethod(() => new DocumentDbSerializer(_typeMapper)),
                Singleton.For<IRemotableMessageSerializer>().UsingFactoryMethod(() => new RemotableMessageSerializer(_typeMapper)),
                Singleton.For<IEventstoreEventPublisher>().UsingFactoryMethod((IInterprocessTransport interprocessTransport, IMessageHandlerRegistry messageHandlerRegistry) => new EventstoreEventPublisher(interprocessTransport, messageHandlerRegistry)),

                Scoped.For<IRemoteApiNavigatorSession>().UsingFactoryMethod((IInterprocessTransport interprocessTransport) => new RemoteApiBrowserSession(interprocessTransport)));

            if(configuration.HasMessageHandlers)
            {
                _container.Register(
                    Singleton.For<IInbox>().UsingFactoryMethod((IServiceLocator serviceLocator, EndpointConfiguration endpointConfiguration, ITaskRunner taskRunner, IRemotableMessageSerializer serializer) => new Inbox(serviceLocator, globalStateTracker, registry, endpointConfiguration, endpointSqlConnection, _typeMapper, taskRunner, serializer)),
                    Singleton.For<CommandScheduler>().UsingFactoryMethod((IInterprocessTransport transport, IUtcTimeTimeSource timeSource) => new CommandScheduler(transport, timeSource)),
                    Singleton.For<IAggregateTypeValidator>().UsingFactoryMethod(() => new AggregateTypeValidator(_typeMapper)),

                    Scoped.For<IServiceBusSession, ILocalApiNavigatorSession>().UsingFactoryMethod((IInterprocessTransport interprocessTransport, CommandScheduler commandScheduler, IMessageHandlerRegistry messageHandlerRegistry, IRemoteApiNavigatorSession remoteNavigator) => new ApiNavigatorSession(interprocessTransport, commandScheduler, messageHandlerRegistry, remoteNavigator))
                );
            }

            if(_container.RunMode == RunMode.Production)
            {
                _container.Register(Singleton.For<IUtcTimeTimeSource>().UsingFactoryMethod(() => new DateTimeNowTimeSource()).DelegateToParentServiceLocatorWhenCloning());
            } else
            {
                _container.Register(Singleton.For<IUtcTimeTimeSource, TestingTimeSource>().UsingFactoryMethod(() => TestingTimeSource.FollowingSystemClock).DelegateToParentServiceLocatorWhenCloning());
            }
        }
    }
}
