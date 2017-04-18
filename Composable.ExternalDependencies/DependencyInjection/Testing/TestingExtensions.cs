using System;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Buses;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.Serialization.NewtonSoft;
using Composable.System.Configuration;
using Composable.System.Linq;
using Composable.SystemExtensions.Threading;

namespace Composable.DependencyInjection.Testing
{
    static class TestingExtensions
    {

        static readonly Lazy<string> MasterDbConnectionString = new Lazy<string>(() => new AppConfigConnectionStringProvider().GetConnectionString("MasterDB")
                                                                                                                                           .ConnectionString);
        /// <summary>
        /// <para>SingleThreadUseGuard is registered for the component ISingleContextUseGuard</para>
        /// </summary>
        public static void ConfigureWiringForTestsCallBeforeAllOtherWiring(this IDependencyInjectionContainer @this, TestingMode mode = TestingMode.RealComponents)
        {
            var masterConnectionString = MasterDbConnectionString.Value;//evaluate lazy here in order to not pollute profiler timings of component resolution or registering.
            var dummyTimeSource = DummyTimeSource.Now;
            var registry = new MessageHandlerRegistry();
            var bus = new TestingOnlyServiceBus(dummyTimeSource, registry);
            var runMode = new RunMode(isTesting:true, mode:mode);

            @this.Register(Component.For<IRunMode>()
                                    .UsingFactoryMethod(_ => runMode)
                                    .LifestyleSingleton(),
                           Component.For<ISingleContextUseGuard>()
                                    .ImplementedBy<SingleThreadUseGuard>()
                                    .LifestyleScoped(),
                           Component.For<IEventStoreEventSerializer>()
                                    .ImplementedBy<NewtonSoftEventStoreEventSerializer>()
                                    .LifestyleScoped(),
                           Component.For<IUtcTimeTimeSource, DummyTimeSource>()
                                    .UsingFactoryMethod(_ => dummyTimeSource)
                                    .LifestyleSingleton()
                                    .DelegateToParentServiceLocatorWhenCloning(),
                           Component.For<IMessageHandlerRegistrar>()
                                    .UsingFactoryMethod(_ => registry)
                                    .LifestyleSingleton(),
                           Component.For<IServiceBus, IMessageSpy>()
                                    .UsingFactoryMethod(_ => bus)
                                    .LifestyleSingleton(),
                           Component.For<IConnectionStringProvider>()
                                    .UsingFactoryMethod(locator => new SqlServerDatabasePoolConnectionStringProvider(masterConnectionString))
                                    .LifestyleSingleton()
                                    .DelegateToParentServiceLocatorWhenCloning()
            );
        }


        public static IServiceLocator Clone(this IServiceLocator @this)
        {
            var sourceContainer = (IDependencyInjectionContainer)@this;

            var cloneContainer = DependencyInjectionContainer.Create();

            sourceContainer.RegisteredComponents()
                           .ForEach(componentRegistration => cloneContainer.Register(componentRegistration.CreateCloneRegistration(@this)));

            return cloneContainer.CreateServiceLocator();
        }
    }
}
