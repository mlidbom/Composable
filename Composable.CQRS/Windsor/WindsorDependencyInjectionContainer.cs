using System;
using System.Linq;
using Castle.Core.Internal;
using Castle.MicroKernel.Lifestyle;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.DependencyInjection;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Buses;
using Composable.System.Configuration;
using Composable.Windsor.Testing;

namespace Composable.Windsor
{
    public static class WindsorDependencyInjectionContainerExtensions
    {
        internal static IDependencyInjectionContainer AsDependencyInjectionContainer(this IWindsorContainer @this) => new WindsorDependencyInjectionContainer(@this);
        public static IWindsorContainer Unsupported(this IDependencyInjectionContainer @this) => ((WindsorDependencyInjectionContainer)@this).WindsorContainer;
        public static IWindsorContainer Unsupported(this IServiceLocator @this) => ((WindsorServiceLocator)@this).WindsorContainer;
    }

    public static class WindsorDependencyInjectionContainerFactory
    {
        public static IServiceLocator SetupForTesting(Action<IDependencyInjectionContainer> setup)
        {
            var @this = new WindsorDependencyInjectionContainer(new WindsorContainer());


            @this.Unsupported().ConfigureWiringForTestsCallBeforeAllOtherWiring();

            var dummyTimeSource = DummyTimeSource.Now;
            var registry = new MessageHandlerRegistry();
            var bus = new TestingOnlyServiceBus(dummyTimeSource, registry);

            @this.Register(
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
                                     .Instance(@this.Unsupported())
                                     .LifestyleSingleton(),
                           CComponent.For<IConnectionStringProvider>()
                                     .Instance(new DummyConnectionStringProvider())
                                     .LifestyleSingleton()
                          );

            setup(@this);

            @this.Unsupported().ConfigureWiringForTestsCallAfterAllOtherWiring();

            return @this.CreateServiceLocator();
        }
    }

    class WindsorDependencyInjectionContainer : IDependencyInjectionContainer
    {
        internal readonly IWindsorContainer WindsorContainer;
        public WindsorDependencyInjectionContainer(IWindsorContainer windsorContainer) { WindsorContainer = windsorContainer; }
        public IDependencyInjectionContainer Register(params CComponentRegistration[] registration)
        {
            var windsorRegistrations = registration.Select(ToWindsorRegistration)
                                                   .ToArray();

            WindsorContainer.Register(windsorRegistrations);
            return this;
        }
        public IServiceLocator CreateServiceLocator() => new WindsorServiceLocator(WindsorContainer);

        public bool IsTestMode => WindsorContainer.Kernel.HasComponent(typeof(TestModeMarker));

        IRegistration ToWindsorRegistration(CComponentRegistration componentRegistration)
        {
            ComponentRegistration<object> registration = Component.For(componentRegistration.ServiceTypes);

            if(componentRegistration.InstantiationSpec.Instance != null)
            {
                registration.Instance(componentRegistration.InstantiationSpec.Instance);
            }else if(componentRegistration.InstantiationSpec.ImplementationType != null)
            {
                registration.ImplementedBy(componentRegistration.InstantiationSpec.ImplementationType);
            } else
            {
                throw new Exception($"Invalid {nameof(InstantiationSpec)}");
            }


            if(!componentRegistration.Name.IsNullOrEmpty())
            {
                registration = registration.Named(componentRegistration.Name);
            }

            switch(componentRegistration.Lifestyle)
            {
                case Lifestyle.Singleton:
                    return registration.LifestyleSingleton();
                case Lifestyle.Scoped:
                    return registration.LifestyleScoped();
                default:
                    throw new ArgumentOutOfRangeException(nameof(componentRegistration.Lifestyle));
            }
        }
        public IComponentLease<TComponent> Lease<TComponent>() => new WindsorComponentLease<TComponent>(WindsorContainer.Resolve<TComponent>(), WindsorContainer);
        public IMultiComponentLease<TComponent> LeaseAll<TComponent>() => new WindsorMultiComponentLease<TComponent>(WindsorContainer.ResolveAll<TComponent>().ToArray(), WindsorContainer);
        public IDisposable BeginScope() => WindsorContainer.BeginScope();
        public void Dispose() => WindsorContainer.Dispose();
    }
}