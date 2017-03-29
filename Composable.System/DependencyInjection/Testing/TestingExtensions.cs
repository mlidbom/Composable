using Composable.GenericAbstractions.Time;
using Composable.Messaging.Buses;
using Composable.System.Configuration;
using Composable.System.Linq;
using Composable.SystemExtensions.Threading;

namespace Composable.DependencyInjection.Testing
{
    static class TestingExtensions
    {
        /// <summary>
        /// <para>SingleThreadUseGuard is registered for the component ISingleContextUseGuard</para>
        /// </summary>
        public static void ConfigureWiringForTestsCallBeforeAllOtherWiring(this IDependencyInjectionContainer @this)
        {
            var dummyTimeSource = DummyTimeSource.Now;
            var registry = new MessageHandlerRegistry();
            var bus = new TestingOnlyServiceBus(dummyTimeSource, registry);

            @this.Register(Component.For<TestModeMarker>()
                                     .ImplementedBy<TestModeMarker>()
                                     .LifestyleSingleton(),
                           Component.For<ISingleContextUseGuard>()
                                     .ImplementedBy<SingleThreadUseGuard>()
                                     .LifestyleScoped(),
                           Component.For<IUtcTimeTimeSource, DummyTimeSource>()
                                     .Instance(dummyTimeSource)
                                     .LifestyleSingleton(),
                           Component.For<IMessageHandlerRegistrar>()
                                     .Instance(registry)
                                     .LifestyleSingleton(),
                           Component.For<IServiceBus, IMessageSpy>()
                                     .Instance(bus)
                                     .LifestyleSingleton(),
                           Component.For<IConnectionStringProvider>()
                                     .Instance(new DummyConnectionStringProvider())
                                     .LifestyleSingleton()
                          );
        }

        public static void ConfigureWiringForTestsCallAfterAllOtherWiring(this IDependencyInjectionContainer container)
        {
            container.CreateServiceLocator()
                     .UseAll<IConfigureWiringForTests>(components
                                                           => components.ForEach(component => component.ConfigureWiringForTesting()));
        }
    }
}
