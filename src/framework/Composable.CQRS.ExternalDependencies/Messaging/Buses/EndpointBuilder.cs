using Composable.DependencyInjection;
using Composable.GenericAbstractions.Time;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.Serialization.NewtonSoft;
using Composable.SystemExtensions.Threading;

namespace Composable.Messaging.Buses
{
    class EndpointBuilder : IEndpointBuilder
    {
        readonly IDependencyInjectionContainer _container;
        readonly MessageHandlerRegistry _registry;

        public EndpointBuilder(string name, IRunMode mode, IGlobalBusStrateTracker globalStateTracker)
        {
            _container = DependencyInjectionContainer.Create(mode);

            _registry = new MessageHandlerRegistry();

            var dummyTimeSource = DummyTimeSource.Now;
            var inprocessBus = new InProcessServiceBus(_registry);

            var serviceBus = new InterprocessServiceBus(name, dummyTimeSource, inprocessBus, globalStateTracker);

            _container.Register(Component.For<ISingleContextUseGuard>()
                                         .ImplementedBy<SingleThreadUseGuard>()
                                         .LifestyleScoped(),
                                Component.For<IEventStoreEventSerializer>()
                                         .ImplementedBy<NewtonSoftEventStoreEventSerializer>()
                                         .LifestyleScoped(),
                                Component.For<IUtcTimeTimeSource>()
                                         .UsingFactoryMethod(factoryMethod: _ => DateTimeNowTimeSource.Instance)
                                         .LifestyleSingleton()
                                         .DelegateToParentServiceLocatorWhenCloning(),
                                Component.For<IMessageHandlerRegistrar, MessageHandlerRegistry>()
                                         .UsingFactoryMethod(factoryMethod: _ => _registry)
                                         .LifestyleSingleton(),
                                Component.For<IInProcessServiceBus, IMessageSpy>()
                                         .UsingFactoryMethod(_ => inprocessBus)
                                         .LifestyleSingleton(),
                                Component.For<IInterProcessServiceBus, InterprocessServiceBus>()
                                         .UsingFactoryMethod(factoryMethod: _ => serviceBus)
                                         .LifestyleSingleton(),
                                Component.For<IGlobalBusStrateTracker>()
                                         .UsingFactoryMethod(_ => globalStateTracker)
                                         .LifestyleSingleton());
        }

        public IDependencyInjectionContainer Container => _container;
        public IMessageHandlerRegistrar MessageHandlerRegistrar => _registry;
        public IEndpoint Build() => new Endpoint(_container.CreateServiceLocator());
    }
}
