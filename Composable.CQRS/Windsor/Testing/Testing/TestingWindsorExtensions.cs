using System;
using Castle.Windsor;
using Composable.DependencyInjection;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Buses;
using Composable.System.Configuration;

namespace Composable.Windsor.Testing.Testing
{
    public static class TestingWindsorExtensions
    {
        public static IWindsorContainer SetupForTesting(this IWindsorContainer @this, Action<IWindsorContainer> registerComponents)
        {
            @this.ConfigureWiringForTestsCallBeforeAllOtherWiring();

            var dummyTimeSource = DummyTimeSource.Now;
            var registry = new MessageHandlerRegistry();
            var bus = new TestingOnlyServiceBus(dummyTimeSource, registry);

            @this.AsDependencyInjectionContainer()
                 .Register(
                           CComponent.For<IUtcTimeTimeSource, DummyTimeSource>()
                                    .Instance(dummyTimeSource)
                                    .LifestyleSingleton(),
                           CComponent.For<IMessageHandlerRegistrar>()
                                    .Instance(registry)
                                    .LifestyleSingleton(),
                           CComponent.For<IServiceBus, IMessageSpy>()
                                    .Instance(bus)
                                    .LifestyleSingleton(),
                           CComponent.For<IWindsorContainer>()
                                    .Instance(@this)
                                    .LifestyleSingleton(),
                           CComponent.For<IConnectionStringProvider>()
                                    .Instance(new DummyConnectionStringProvider())
                                    .LifestyleSingleton()
                );

            registerComponents(@this);

            @this.ConfigureWiringForTestsCallAfterAllOtherWiring();

            return @this;
        }
    }
}
