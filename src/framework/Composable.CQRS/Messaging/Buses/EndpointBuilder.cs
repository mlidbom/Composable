using System;
using System.Configuration;
using Composable.DependencyInjection;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Buses.Implementation;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.Aggregates;
using Composable.Refactoring.Naming;
using Composable.Serialization;
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
            return new Endpoint(_container.CreateServiceLocator(), Configuration);
        }

        void SetupInternalTypeMap()
        {
            EventStoreApi.MapTypes(TypeMapper);
            BusApi.MapTypes(TypeMapper);
        }

        public EndpointBuilder(IGlobalBusStateTracker globalStateTracker, IDependencyInjectionContainer container, string name, EndpointId endpointId)
        {
            _container = container;

            Configuration = new EndpointConfiguration(name, endpointId);

            var endpointSqlConnection = container.RunMode.IsTesting
                                            ? new LazySqlServerConnection(new Lazy<string>(() => container.CreateServiceLocator().Resolve<ISqlConnectionProvider>().GetConnectionProvider(Configuration.ConnectionStringName).ConnectionString))
                                            : new SqlServerConnection(ConfigurationManager.ConnectionStrings[Configuration.ConnectionStringName].ConnectionString);

            _typeMapper = new TypeMapper(endpointSqlConnection);

            var registry = new MessageHandlerRegistry(_typeMapper);
            RegisterHandlers = new MessageHandlerRegistrarWithDependencyInjectionSupport(registry, new Lazy<IServiceLocator>(() => _container.CreateServiceLocator()));

            _container.Register(
                Component.For<ITaskRunner>()
                         .UsingFactoryMethod(() => new TaskRunner())
                         .LifestyleSingleton(),
                Component.For<EndpointId>().UsingFactoryMethod(() => endpointId).LifestyleSingleton(),
                Component.For<EndpointConfiguration>()
                         .UsingFactoryMethod(() => Configuration)
                         .LifestyleSingleton(),
                Component.For<ITypeMappingRegistar, ITypeMapper, TypeMapper>()
                         .UsingFactoryMethod(() => _typeMapper)
                         .LifestyleSingleton()
                         .DelegateToParentServiceLocatorWhenCloning(),
                Component.For<IAggregateTypeValidator>()
                         .UsingFactoryMethod(() => new AggregateTypeValidator(_typeMapper))
                         .LifestyleSingleton(),
                Component.For<IInterprocessTransport>()
                         .UsingFactoryMethod((IUtcTimeTimeSource timeSource, ISqlConnectionProvider connectionProvider, EndpointId id, ITaskRunner taskRunner, IRemotableMessageSerializer serializer) =>
                                                 new InterprocessTransport(globalStateTracker, timeSource, endpointSqlConnection, _typeMapper, id, taskRunner, serializer))
                         .LifestyleSingleton(),
                Component.For<ISingleContextUseGuard>()
                         .UsingFactoryMethod(() => new SingleThreadUseGuard())
                         .LifestyleScoped(),
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
                Component.For<IInbox>()
                         .UsingFactoryMethod((IServiceLocator serviceLocator, IGlobalBusStateTracker stateTracker, EndpointConfiguration endpointConfiguration, ITaskRunner taskRunner, IRemotableMessageSerializer serializer) =>
                                                 new Inbox(serviceLocator, stateTracker, registry, endpointConfiguration, endpointSqlConnection, _typeMapper, taskRunner, serializer))
                         .LifestyleSingleton(),
                Component.For<CommandScheduler>()
                         .UsingFactoryMethod((IInterprocessTransport transport, IUtcTimeTimeSource timeSource) => new CommandScheduler(transport, timeSource))
                         .LifestyleSingleton(),
                Component.For<IServiceBusControl>()
                         .UsingFactoryMethod((IInterprocessTransport interprocessTransport, IInbox inbox, CommandScheduler commandScheduler) => new ServiceBusControl(interprocessTransport, inbox, commandScheduler))
                         .LifestyleSingleton(),
                Component.For<IServiceBusSession, IRemoteApiNavigatorSession, ILocalApiNavigatorSession>()
                         .UsingFactoryMethod((IInterprocessTransport interprocessTransport, CommandScheduler commandScheduler, IMessageHandlerRegistry messageHandlerRegistry) =>
                                                 new ApiNavigatorSession(interprocessTransport, commandScheduler, messageHandlerRegistry))
                         .LifestyleScoped(),
                Component.For<IEventstoreEventPublisher>()
                         .UsingFactoryMethod((IInterprocessTransport interprocessTransport, IMessageHandlerRegistry messageHandlerRegistry) => new EventstoreEventPublisher(interprocessTransport, messageHandlerRegistry))
                         .LifestyleScoped(),
                Component.For<ISqlConnectionProvider>()
                         .UsingFactoryMethod(() => new SqlServerDatabasePoolSqlConnectionProvider())
                         .LifestyleSingleton()
                         .DelegateToParentServiceLocatorWhenCloning());

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
