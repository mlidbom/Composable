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

        public EndpointBuilder(IRunMode mode)
        {
            _container = DependencyInjectionContainer.Create(mode);

            _registry = new MessageHandlerRegistry();

            var dummyTimeSource = DummyTimeSource.Now;
            var inprocessBus = new InProcessServiceBus(_registry);
            var testingOnlyServiceBus = new TestingOnlyInterprocessServiceBus(dummyTimeSource, inprocessBus);

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
                                Component.For<IInterProcessServiceBus, TestingOnlyInterprocessServiceBus>()
                                         .UsingFactoryMethod(factoryMethod: _ => testingOnlyServiceBus)
                                         .LifestyleSingleton());
        }

        public IDependencyInjectionContainer Container => _container;
        public IMessageHandlerRegistrar MessageHandlerRegistrar => _registry;
        public IEndpoint Build() => new Endpoint(_container.CreateServiceLocator());
    }
}
