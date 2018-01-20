using Composable.DependencyInjection;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Buses.Implementation;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.AggregateRoots;
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
        TypeMapper _typeMapper;

        public EndpointBuilder(IGlobalBusStateTracker globalStateTracker, IDependencyInjectionContainer container, string name)
        {
            _container = container;
            _typeMapper = new TypeMapper();

            Configuration = new EndpointConfiguration(name)
                                {};

            _container.Register(
                Component.For<EndpointConfiguration>()
                         .UsingFactoryMethod(() => Configuration)
                         .LifestyleSingleton(),
                Component.For<ITypeMappingRegistar, ITypeMapper, TypeMapper>()
                         .UsingFactoryMethod(() => _typeMapper)
                         .LifestyleSingleton()
                         .DelegateToParentServiceLocatorWhenCloning(),
                Component.For<IAggregateTypeValidator>()
                         .ImplementedBy<AggregateTypeValidator>()
                         .LifestyleSingleton(),
                Component.For<IInterprocessTransport>()
                         .UsingFactoryMethod((IUtcTimeTimeSource timeSource, ISqlConnectionProvider connectionProvider, ITypeMapper typeMapper) =>
                                                 new InterprocessTransport(globalStateTracker, timeSource, connectionProvider.GetConnectionProvider(Configuration.ConnectionStringName), typeMapper))
                         .LifestyleSingleton(),
                Component.For<ISingleContextUseGuard>()
                         .ImplementedBy<SingleThreadUseGuard>()
                         .LifestyleScoped(),
                Component.For<IGlobalBusStateTracker>()
                         .UsingFactoryMethod(() => globalStateTracker)
                         .LifestyleSingleton(),
                Component.For<IMessageHandlerRegistry, IMessageHandlerRegistrar, MessageHandlerRegistry>()
                         .UsingFactoryMethod((ITypeMapper typeMapper) => new MessageHandlerRegistry(typeMapper))
                         .LifestyleSingleton(),
                Component.For<IEventStoreEventSerializer>()
                         .ImplementedBy<NewtonSoftEventStoreEventSerializer>()
                         .LifestyleScoped(),
                Component.For<IUtcTimeTimeSource>()
                         .UsingFactoryMethod(() => new DateTimeNowTimeSource())
                         .LifestyleSingleton()
                         .DelegateToParentServiceLocatorWhenCloning(),
                Component.For<IInProcessServiceBus>()
                         .UsingFactoryMethod((IMessageHandlerRegistry registry) => new InProcessServiceBus(registry))
                         .LifestyleSingleton(),
                Component.For<IInbox>()
                         .UsingFactoryMethod(k => new Inbox(k.Resolve<IServiceLocator>(), k.Resolve<IGlobalBusStateTracker>(), k.Resolve<IMessageHandlerRegistry>(), k.Resolve<EndpointConfiguration>(), k.Resolve<ISqlConnectionProvider>().GetConnectionProvider(Configuration.ConnectionStringName), k.Resolve<ITypeMapper>()))
                         .LifestyleSingleton(),
                Component.For<CommandScheduler>()
                         .UsingFactoryMethod((IInterprocessTransport transport, IUtcTimeTimeSource timeSource) => new CommandScheduler(transport, timeSource))
                         .LifestyleSingleton(),
                Component.For<IServiceBus, IServiceBusControl>()
                         .UsingFactoryMethod((IInterprocessTransport transport, IInbox inbox, CommandScheduler scheduler) => new ServiceBus(transport, inbox, scheduler))
                         .LifestyleSingleton(),
                Component.For<ISqlConnectionProvider>()
                         .UsingFactoryMethod(() => new SqlServerDatabasePoolSqlConnectionProvider(MasterDbConnection.ConnectionString))
                         .LifestyleSingleton()
                         .DelegateToParentServiceLocatorWhenCloning());
        }

        public IDependencyInjectionContainer Container => _container;
        public ITypeMappingRegistar TypeMapper => _typeMapper;
        public EndpointConfiguration Configuration { get; }

        public MessageHandlerRegistrarWithDependencyInjectionSupport RegisterHandlers =>
            new MessageHandlerRegistrarWithDependencyInjectionSupport(_container.CreateServiceLocator().Resolve<IMessageHandlerRegistrar>(), _container.CreateServiceLocator());

        public IEndpoint Build(string name, EndpointId id) => new Endpoint(_container.CreateServiceLocator(), id, name);
    }
}
