using System;
using System.Configuration;
using Composable.DependencyInjection;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Buses.Implementation;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.Aggregates;
using Composable.Persistence.EventStore.Serialization.NewtonSoft;
using Composable.Refactoring.Naming;
using Composable.System.Configuration;
using Composable.System.Data.SqlClient;
using Composable.System.Threading;
using Composable.SystemExtensions.Threading;

namespace Composable.Messaging.Buses
{
    class EndpointBuilder : IEndpointBuilder
    {
        static readonly ISqlConnection MasterDbConnection = new AppConfigSqlConnectionProvider().GetConnectionProvider(parameterName: "MasterDB");

        readonly IDependencyInjectionContainer _container;
        readonly string _name;
        readonly TypeMapper _typeMapper;
        readonly EndpointId _endpointId;

        public IDependencyInjectionContainer Container => _container;
        public ITypeMappingRegistar TypeMapper => _typeMapper;
        public EndpointConfiguration Configuration { get; }

        public MessageHandlerRegistrarWithDependencyInjectionSupport RegisterHandlers { get; }

        public IEndpoint Build() => new Endpoint(_container.CreateServiceLocator(), _endpointId, _name);

        public EndpointBuilder(IGlobalBusStateTracker globalStateTracker, IDependencyInjectionContainer container, string name, EndpointId endpointId)
        {
            _container = container;
            _name = name;
            _endpointId = endpointId;
            _typeMapper = new TypeMapper();
            var registry = new MessageHandlerRegistry(_typeMapper);

            Configuration = new EndpointConfiguration(name);

            RegisterHandlers = new MessageHandlerRegistrarWithDependencyInjectionSupport(registry, new Lazy<IServiceLocator>(() => _container.CreateServiceLocator()));

            DefaultWiring(globalStateTracker, _container, endpointId, Configuration, _typeMapper, registry);
        }

        internal static void DefaultWiring(IGlobalBusStateTracker globalStateTracker, IDependencyInjectionContainer container, EndpointId endpointId, EndpointConfiguration configuration, TypeMapper typeMapper, MessageHandlerRegistry registry)
        {
            var sqlServerConnection = container.RunMode.IsTesting
                                          ? new LazySqlServerConnection(new Lazy<string>(() => container.CreateServiceLocator().Resolve<ISqlConnectionProvider>().GetConnectionProvider(configuration.ConnectionStringName).ConnectionString))
                                          : new SqlServerConnection(ConfigurationManager.ConnectionStrings[configuration.ConnectionStringName].ConnectionString);

            container.Register(
                Component.For<ITaskRunner>().ImplementedBy<TaskRunner>().LifestyleSingleton(),
                Component.For<EndpointId>().UsingFactoryMethod(() => endpointId).LifestyleSingleton(),
                Component.For<EndpointConfiguration>()
                         .UsingFactoryMethod(() => configuration)
                         .LifestyleSingleton(),
                Component.For<ITypeMappingRegistar, ITypeMapper, TypeMapper>()
                         .UsingFactoryMethod(() => typeMapper)
                         .LifestyleSingleton()
                         .DelegateToParentServiceLocatorWhenCloning(),
                Component.For<IAggregateTypeValidator>()
                         .ImplementedBy<AggregateTypeValidator>()
                         .LifestyleSingleton(),
                Component.For<IInterprocessTransport>()
                         .UsingFactoryMethod((IUtcTimeTimeSource timeSource, ISqlConnectionProvider connectionProvider, EndpointId id, ITaskRunner taskRunner) =>
                                                 new InterprocessTransport(globalStateTracker, timeSource, sqlServerConnection, typeMapper, id, taskRunner))
                         .LifestyleSingleton(),
                Component.For<ISingleContextUseGuard>()
                         .ImplementedBy<SingleThreadUseGuard>()
                         .LifestyleScoped(),
                Component.For<IGlobalBusStateTracker>()
                         .UsingFactoryMethod(() => globalStateTracker)
                         .LifestyleSingleton(),
                Component.For<IMessageHandlerRegistry, IMessageHandlerRegistrar, MessageHandlerRegistry>()
                         .UsingFactoryMethod(() => registry)
                         .LifestyleSingleton(),
                Component.For<IEventStoreEventSerializer>()
                         .ImplementedBy<NewtonSoftEventStoreEventSerializer>()
                         .LifestyleScoped(),
                Component.For<IInbox>()
                         .UsingFactoryMethod(k => new Inbox(k.Resolve<IServiceLocator>(), k.Resolve<IGlobalBusStateTracker>(), k.Resolve<IMessageHandlerRegistry>(), k.Resolve<EndpointConfiguration>(), sqlServerConnection, k.Resolve<ITypeMapper>(), k.Resolve<ITaskRunner>()))
                         .LifestyleSingleton(),
                Component.For<CommandScheduler>()
                         .UsingFactoryMethod((IInterprocessTransport transport, IUtcTimeTimeSource timeSource) => new CommandScheduler(transport, timeSource))
                         .LifestyleSingleton(),
                Component.For<IServiceBusControl>()
                         .ImplementedBy<ServiceBusControl>()
                         .LifestyleSingleton(),
                Component.For<IServiceBusSession, IRemoteApiNavigatorSession, ILocalApiNavigatorSession>()
                         .ImplementedBy<ApiNavigatorSession>()
                         .LifestyleScoped(),
                Component.For<IEventstoreEventPublisher>()
                         .ImplementedBy<EventstoreEventPublisher>()
                         .LifestyleScoped(),
                Component.For<ISqlConnectionProvider>()
                         .UsingFactoryMethod(() => new SqlServerDatabasePoolSqlConnectionProvider(MasterDbConnection.ConnectionString))
                         .LifestyleSingleton()
                         .DelegateToParentServiceLocatorWhenCloning());

            if(container.RunMode == RunMode.Production)
            {
                container.Register(Component.For<IUtcTimeTimeSource>()
                                            .UsingFactoryMethod(() => new DateTimeNowTimeSource())
                                            .LifestyleSingleton()
                                            .DelegateToParentServiceLocatorWhenCloning());
            } else
            {
                container.Register(Component.For<IUtcTimeTimeSource, TestingTimeSource>()
                                            .UsingFactoryMethod(() => TestingTimeSource.FollowingSystemClock)
                                            .LifestyleSingleton()
                                            .DelegateToParentServiceLocatorWhenCloning());
            }
        }
    }
}
