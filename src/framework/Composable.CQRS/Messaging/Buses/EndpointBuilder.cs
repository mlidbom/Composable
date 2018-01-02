using Composable.DependencyInjection;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Buses.Implementation;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.Serialization.NewtonSoft;
using Composable.System.Configuration;
using Composable.System.Data.SqlClient;
using Composable.SystemExtensions.Threading;

namespace Composable.Messaging.Buses
{
    class EndpointBuilder : IEndpointBuilder
    {
        static readonly ISqlConnection MasterDbConnection = new AppConfigSqlConnectionProvider().GetConnectionProvider(parameterName: "MasterDB");

        readonly IDependencyInjectionContainer _container;

        public EndpointBuilder(IGlobalBusStateTracker globalStateTracker, IDependencyInjectionContainer container, string name)
        {
            _container = container;

            Configuration = new EndpointConfiguration(name)
                            {
                            };

            _container.Register(
                Component.For<EndpointConfiguration>()
                         .UsingFactoryMethod(() => Configuration)
                         .LifestyleSingleton(),
                Component.For<IInterprocessTransport>()
                         .UsingFactoryMethod((IUtcTimeTimeSource timeSource) => new InterprocessTransport(globalStateTracker, timeSource))
                         .LifestyleSingleton(),
                Component.For<ISingleContextUseGuard>()
                         .ImplementedBy<SingleThreadUseGuard>()
                         .LifestyleScoped(),
                Component.For<IGlobalBusStateTracker>()
                         .UsingFactoryMethod(() => globalStateTracker)
                         .LifestyleSingleton(),
                Component.For<IMessageHandlerRegistry, IMessageHandlerRegistrar, MessageHandlerRegistry>()
                         .UsingFactoryMethod(() => new MessageHandlerRegistry())
                         .LifestyleSingleton(),
                Component.For<IEventStoreEventSerializer>()
                         .ImplementedBy<NewtonSoftEventStoreEventSerializer>()
                         .LifestyleScoped(),
                Component.For<IUtcTimeTimeSource>()
                         .UsingFactoryMethod(() => new DateTimeNowTimeSource())
                         .LifestyleSingleton()
                         .DelegateToParentServiceLocatorWhenCloning(),
                Component.For<IInProcessServiceBus, IMessageSpy>()
                         .UsingFactoryMethod((IMessageHandlerRegistry registry) => new InProcessServiceBus(registry))
                         .LifestyleSingleton(),
                Component.For<IInbox>()
                         .UsingFactoryMethod((IServiceLocator serviceLocator, IGlobalBusStateTracker stateTracker, IMessageHandlerRegistry messageHandlerRegistry, EndpointConfiguration configuration) => new Inbox(serviceLocator, stateTracker, messageHandlerRegistry, configuration))
                         .LifestyleSingleton(),
                Component.For<CommandScheduler>()
                         .UsingFactoryMethod((IInterprocessTransport transport, IUtcTimeTimeSource timeSource) => new CommandScheduler(transport, timeSource))
                         .LifestyleSingleton(),
                Component.For<IServiceBus, ISimpleServiceBus, IServiceBusControl>()
                         .UsingFactoryMethod((IInterprocessTransport transport, IInbox inbox, CommandScheduler scheduler) => new ServiceBus(transport, inbox, scheduler))
                         .LifestyleSingleton(),
                Component.For<ISqlConnectionProvider>()
                         .UsingFactoryMethod(() => new SqlServerDatabasePoolSqlConnectionProvider(MasterDbConnection.ConnectionString))
                         .LifestyleSingleton()
                         .DelegateToParentServiceLocatorWhenCloning());
        }

        public IDependencyInjectionContainer Container => _container;
        public EndpointConfiguration Configuration { get; }

        public MessageHandlerRegistrarWithDependencyInjectionSupport RegisterHandlers =>
            new MessageHandlerRegistrarWithDependencyInjectionSupport(_container.CreateServiceLocator().Resolve<IMessageHandlerRegistrar>(), _container.CreateServiceLocator());

        public IEndpoint Build(string name, EndpointId id) => new Endpoint(_container.CreateServiceLocator(), id, name);
    }
}
