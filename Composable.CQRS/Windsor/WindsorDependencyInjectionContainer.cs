using System;
using System.Linq;
using Castle.Core.Internal;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.DependencyInjection;

namespace Composable.Windsor
{
    static class WindsorDependencyInjectionContainerExtensions
    {
        internal static IDependencyInjectionContainer AsDependencyInjectionContainer(this IWindsorContainer @this) => new WindsorDependencyInjectionContainer(@this);
        internal static IWindsorContainer Unsupported(this IDependencyInjectionContainer @this) { return ((WindsorDependencyInjectionContainer)@this).WindsorContainer; }
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
    }
}