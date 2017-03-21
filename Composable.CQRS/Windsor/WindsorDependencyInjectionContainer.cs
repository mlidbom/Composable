using System;
using System.Linq;
using Castle.Core.Internal;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.DependencyInjection;
using Component = Castle.MicroKernel.Registration.Component;
using ComponentRegistration = Composable.DependencyInjection.ComponentRegistration;

namespace Composable.Windsor
{
    class WindsorDependencyInjectionContainer : IDependencyInjectionContainer
    {
        readonly IWindsorContainer _container;
        public WindsorDependencyInjectionContainer(IWindsorContainer container) { _container = container; }
        public IDependencyInjectionContainer Register(params ComponentRegistration[] registration)
        {
            var windsorRegistrations = registration.Select(ToWindsorRegistration)
                                                   .ToArray();

            _container.Register(windsorRegistrations);
            return this;
        }

        IRegistration ToWindsorRegistration(ComponentRegistration componentRegistration)
        {
            var registration = Component.For(componentRegistration.ServiceTypes)
                                        .ImplementedBy(componentRegistration.ImplementingType);

            if(!componentRegistration.Name.IsNullOrEmpty())
            {
                registration = registration.Named(componentRegistration.Name);
            }

            switch(componentRegistration.Lifestyle)
            {
                case LifeStyle.Singleton:
                    return registration.LifestyleSingleton();
                case LifeStyle.Scoped:
                    return registration.LifestyleScoped();
                default:
                    throw new ArgumentOutOfRangeException(nameof(componentRegistration.Lifestyle));
            }
        }
    }
}