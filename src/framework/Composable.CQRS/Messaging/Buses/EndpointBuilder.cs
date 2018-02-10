using System;
using System.Configuration;
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
using Composable.SystemExtensions.Threading;

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
                Component.For<ISqlConnectionProvider>()
                         .UsingFactoryMethod(()=> connectionProvider)
                         .LifestyleSingleton()
                         .DelegateToParentServiceLocatorWhenCloning(),
                Component.For<ITaskRunner>()
                         .UsingFactoryMethod(() => new TaskRunner())
                         .LifestyleSingleton(),
                Component.For<EndpointId>().UsingFactoryMethod(() => configuration.Id).LifestyleSingleton(),
                Component.For<EndpointConfiguration>()
                         .UsingFactoryMethod(() => Configuration)
                         .LifestyleSingleton(),
                Component.For<ITypeMappingRegistar, ITypeMapper, TypeMapper>()
                         .UsingFactoryMethod(() => _typeMapper)
                         .LifestyleSingleton()
                         .DelegateToParentServiceLocatorWhenCloning(),
                Component.For<IInterprocessTransport>()
                         .UsingFactoryMethod((IUtcTimeTimeSource timeSource, ITaskRunner taskRunner, IRemotableMessageSerializer serializer) =>
                                                 new InterprocessTransport(globalStateTracker, timeSource, endpointSqlConnection, _typeMapper, configuration, taskRunner, serializer))
                         .LifestyleSingleton(),
                Component.For<IGlobalBusStateTracker>()
                         .UsingFactoryMethod(() => globalStateTracker)
                         .LifestyleSingleton(),
                Component.For<IMessageHandlerRegistry, IMessageHandlerRegistrar, MessageHandlerRegistry>()
                         .UsingFactoryMethod(() => registry)
                         .LifestyleSingleton(),
                Component.For<IEventStoreSerializer>()
                         .UsingFactoryMethod(() => new EventStoreSerializer(_typeMapper))
                         .LifestyleSingleton(),
                Component.For<IDocumentDbSerializer>()
                         .UsingFactoryMethod(() => new DocumentDbSerializer(_typeMapper))
                         .LifestyleSingleton(),
                Component.For<IRemotableMessageSerializer>()
                         .UsingFactoryMethod(() => new RemotableMessageSerializer(_typeMapper))
                         .LifestyleSingleton(),
                Component.For<IRemoteApiNavigatorSession>()
                         .UsingFactoryMethod((IInterprocessTransport interprocessTransport) => new RemoteApiBrowserSession(interprocessTransport))
                         .LifestyleScoped(),
                Component.For<IEventstoreEventPublisher>()
                         .UsingFactoryMethod((IInterprocessTransport interprocessTransport, IMessageHandlerRegistry messageHandlerRegistry) => new EventstoreEventPublisher(interprocessTransport, messageHandlerRegistry))
                         .LifestyleScoped());

            if(configuration.HasMessageHandlers)
            {
                _container.Register(
                    Component.For<IServiceBusSession, ILocalApiNavigatorSession>()
                             .UsingFactoryMethod((IInterprocessTransport interprocessTransport, CommandScheduler commandScheduler, IMessageHandlerRegistry messageHandlerRegistry, IRemoteApiNavigatorSession remoteNavigator) =>
                                                     new ApiNavigatorSession(interprocessTransport, commandScheduler, messageHandlerRegistry, remoteNavigator))
                             .LifestyleScoped(),
                Component.For<IInbox>()
                         .UsingFactoryMethod((IServiceLocator serviceLocator, EndpointConfiguration endpointConfiguration, ITaskRunner taskRunner, IRemotableMessageSerializer serializer) =>
                                                 new Inbox(serviceLocator, globalStateTracker, registry, endpointConfiguration, endpointSqlConnection, _typeMapper, taskRunner, serializer))
                         .LifestyleSingleton(),
                Component.For<CommandScheduler>()
                         .UsingFactoryMethod((IInterprocessTransport transport, IUtcTimeTimeSource timeSource) => new CommandScheduler(transport, timeSource))
                         .LifestyleSingleton(),
                Component.For<IAggregateTypeValidator>()
                         .UsingFactoryMethod(() => new AggregateTypeValidator(_typeMapper))
                         .LifestyleSingleton()
                    );
            }

            if(_container.RunMode == RunMode.Production)
            {
                _container.Register(Component.For<IUtcTimeTimeSource>()
                                             .UsingFactoryMethod(() => new DateTimeNowTimeSource())
                                             .LifestyleSingleton()
                                             .DelegateToParentServiceLocatorWhenCloning());
            } else
            {
                _container.Register(Component.For<IUtcTimeTimeSource, TestingTimeSource>()
                                             .UsingFactoryMethod(() => TestingTimeSource.FollowingSystemClock)
                                             .LifestyleSingleton()
                                             .DelegateToParentServiceLocatorWhenCloning());
            }
        }
    }
}
