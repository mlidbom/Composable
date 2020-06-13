using System;
using System.Collections.Generic;
using System.Linq;
using Castle.MicroKernel;
using Castle.MicroKernel.Lifestyle;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Resolvers.SpecializedResolvers;
using Castle.Windsor;
using Composable.Contracts;

namespace Composable.DependencyInjection.Windsor
{
    // ReSharper disable once ClassNeverInstantiated.Global
    class WindsorDependencyInjectionContainer : IDependencyInjectionContainer, IServiceLocator
    {
        readonly IWindsorContainer _windsorContainer;
        readonly List<ComponentRegistration> _registeredComponents = new List<ComponentRegistration>();
        bool _locked;
        internal WindsorDependencyInjectionContainer(IRunMode runMode)
        {
            RunMode = runMode;
            _windsorContainer = new WindsorContainer();
            _windsorContainer.Kernel.Resolver.AddSubResolver(new CollectionResolver(_windsorContainer.Kernel));
        }

        public IRunMode RunMode { get; }
        public void Register(params ComponentRegistration[] registrations)
        {
            Contract.Assert.That(!_locked, "You cannot modify the container once you have started using it to resolve components");

            _registeredComponents.AddRange(registrations);

            var windsorRegistrations = registrations.Select(ToWindsorRegistration)
                                                   .ToArray();

            _windsorContainer.Register(windsorRegistrations);
        }
        public IEnumerable<ComponentRegistration> RegisteredComponents() => _registeredComponents;

        IServiceLocator IDependencyInjectionContainer.CreateServiceLocator()
        {
            _locked = true;
            return this;
        }

        public TComponent Resolve<TComponent>() where TComponent : class => _windsorContainer.Resolve<TComponent>();
        public TComponent[] ResolveAll<TComponent>() where TComponent : class => _windsorContainer.ResolveAll<TComponent>().ToArray();
        IDisposable IServiceLocator.BeginScope() => _windsorContainer.BeginScope();
        void IDisposable.Dispose() => _windsorContainer.Dispose();

        static IRegistration ToWindsorRegistration(ComponentRegistration componentRegistration)
        {
            Castle.MicroKernel.Registration.ComponentRegistration<object> registration = Castle.MicroKernel.Registration.Component.For(componentRegistration.ServiceTypes);

            if (componentRegistration.InstantiationSpec.Instance != null)
            {
                registration.Instance(componentRegistration.InstantiationSpec.Instance);
            }
            else if (componentRegistration.InstantiationSpec.FactoryMethod != null)
            {
                registration.UsingFactoryMethod(kernel => componentRegistration.InstantiationSpec.FactoryMethod(new WindsorServiceLocatorKernel(kernel)));
            }
            else
            {
                throw new Exception($"Invalid {nameof(InstantiationSpec)}");
            }

            return componentRegistration.Lifestyle switch
            {
                Lifestyle.Singleton => registration.LifestyleSingleton(),
                Lifestyle.Scoped => registration.LifestyleScoped(),
                _ => throw new ArgumentOutOfRangeException(nameof(componentRegistration.Lifestyle))
            };
        }

        sealed class WindsorServiceLocatorKernel : IServiceLocatorKernel
        {
            readonly IKernel _kernel;
            internal WindsorServiceLocatorKernel(IKernel kernel) => _kernel = kernel;

            TComponent IServiceLocatorKernel.Resolve<TComponent>() => _kernel.Resolve<TComponent>();
        }
    }
}