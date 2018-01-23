using Composable.DependencyInjection;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Buses.Implementation;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.Aggregates;
using Composable.Persistence.EventStore.Serialization.NewtonSoft;
using Composable.Refactoring.Naming;
using Composable.System.Configuration;
using Composable.System.Data.SqlClient;
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

        public EndpointBuilder(IGlobalBusStateTracker globalStateTracker, IDependencyInjectionContainer container, string name, EndpointId endpointId)
        {
            _container = container;
            _name = name;
            _endpointId = endpointId;
            _typeMapper = new TypeMapper();

            Configuration = new EndpointConfiguration(name);

            DefaultWiring(globalStateTracker, _container, endpointId, Configuration, _typeMapper);
        }

        internal static void DefaultWiring(IGlobalBusStateTracker globalStateTracker, IDependencyInjectionContainer container, EndpointId endpointId, EndpointConfiguration configuration, TypeMapper typeMapper)
        {
            container.Register(
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
                         .UsingFactoryMethod((IUtcTimeTimeSource timeSource, ISqlConnectionProvider connectionProvider, EndpointId id) =>
                                                 new InterprocessTransport(globalStateTracker, timeSource, connectionProvider.GetConnectionProvider(configuration.ConnectionStringName), typeMapper, id))
                         .LifestyleSingleton(),
                Component.For<ISingleContextUseGuard>()
                         .ImplementedBy<SingleThreadUseGuard>()
                         .LifestyleScoped(),
                Component.For<IGlobalBusStateTracker>()
                         .UsingFactoryMethod(() => globalStateTracker)
                         .LifestyleSingleton(),
                Component.For<IMessageHandlerRegistry, IMessageHandlerRegistrar, MessageHandlerRegistry>()
                         .UsingFactoryMethod(() => new MessageHandlerRegistry(typeMapper))
                         .LifestyleSingleton(),
                Component.For<IEventStoreEventSerializer>()
                         .ImplementedBy<NewtonSoftEventStoreEventSerializer>()
                         .LifestyleScoped(),
                Component.For<IInbox>()
                         .UsingFactoryMethod(k => new Inbox(k.Resolve<IServiceLocator>(), k.Resolve<IGlobalBusStateTracker>(), k.Resolve<IMessageHandlerRegistry>(), k.Resolve<EndpointConfiguration>(), k.Resolve<ISqlConnectionProvider>().GetConnectionProvider(configuration.ConnectionStringName), k.Resolve<ITypeMapper>()))
                         .LifestyleSingleton(),
                Component.For<CommandScheduler>()
                         .UsingFactoryMethod((IInterprocessTransport transport, IUtcTimeTimeSource timeSource) => new CommandScheduler(transport, timeSource))
                         .LifestyleSingleton(),
                Component.For<IServiceBus>()
                         .ImplementedBy<ServiceBus>()
                         .LifestyleSingleton(),
                Component.For<IServiceBusSession, ILocalServiceBusSession, IEventstoreEventPublisher>()
                         .ImplementedBy<ServiceBusSession>()
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

        public IDependencyInjectionContainer Container => _container;
        public ITypeMappingRegistar TypeMapper => _typeMapper;
        public EndpointConfiguration Configuration { get; }

        public MessageHandlerRegistrarWithDependencyInjectionSupport RegisterHandlers =>
            new MessageHandlerRegistrarWithDependencyInjectionSupport(_container.CreateServiceLocator().Resolve<IMessageHandlerRegistrar>(), _container.CreateServiceLocator());

        public IEndpoint Build()
        {
            return new Endpoint(_container.CreateServiceLocator(), _endpointId, _name);
        }
    }
}
