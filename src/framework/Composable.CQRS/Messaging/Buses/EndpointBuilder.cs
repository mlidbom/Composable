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

        public EndpointBuilder(IGlobalBusStrateTracker globalStateTracker, IDependencyInjectionContainer container)
        {
            _container = container;

            _container.Register(
                Component.For<EndpointConfiguration>()
                         .UsingFactoryMethod(() => new EndpointConfiguration())
                         .LifestyleSingleton(),
                Component.For<IInterprocessTransport, InterprocessTransport>()
                         .UsingFactoryMethod(() => new InterprocessTransport(globalStateTracker))
                         .LifestyleSingleton(),
                Component.For<ISingleContextUseGuard>()
                         .ImplementedBy<SingleThreadUseGuard>()
                         .LifestyleScoped(),
                Component.For<IGlobalBusStrateTracker>()
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
                Component.For<IInbox, Inbox>()
                         .ImplementedBy<Inbox>()
                         .LifestyleSingleton(),
                Component.For<IOutbox, Outbox>()
                         .ImplementedBy<Outbox>()
                         .LifestyleSingleton(),
                Component.For<IServiceBus, ServiceBus>()
                         .UsingFactoryMethod((Outbox outbox) => new ServiceBus(outbox))
                         .LifestyleSingleton(),
                Component.For<ISqlConnectionProvider>()
                         .UsingFactoryMethod(() => new SqlServerDatabasePoolSqlConnectionProvider(MasterDbConnection.ConnectionString))
                         .LifestyleSingleton()
                         .DelegateToParentServiceLocatorWhenCloning());
        }

        public IDependencyInjectionContainer Container => _container;

        public MessageHandlerRegistrarWithDependencyInjectionSupport RegisterHandlers =>
            new MessageHandlerRegistrarWithDependencyInjectionSupport(_container.CreateServiceLocator().Resolve<IMessageHandlerRegistrar>(), _container.CreateServiceLocator());

        public IEndpoint Build() => new Endpoint(_container.CreateServiceLocator());
    }
}
