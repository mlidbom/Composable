using System;
using System.Linq;
using Castle.Core.Internal;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.DependencyInjection;
using Component = Castle.MicroKernel.Registration.Component;

namespace Composable.Windsor
{
    static class WindsorDependencyInjectionContainerExtensions
    {
        internal static IDependencyInjectionContainer AsDependencyInjectionContainer(this IWindsorContainer @this) => new WindsorDependencyInjectionContainer(@this);
    }

    class WindsorDependencyInjectionContainer : IDependencyInjectionContainer
    {
        readonly IWindsorContainer _container;
        public WindsorDependencyInjectionContainer(IWindsorContainer container) { _container = container; }
        public IDependencyInjectionContainer Register(params CComponentRegistration[] registration)
        {
            var windsorRegistrations = registration.Select(ToWindsorRegistration)
                                                   .ToArray();

            _container.Register(windsorRegistrations);
            return this;
        }

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