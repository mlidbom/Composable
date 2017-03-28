using System;
using System.Linq;
using Castle.Core.Internal;
using Castle.MicroKernel;
using Castle.MicroKernel.Lifestyle;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Resolvers.SpecializedResolvers;
using Castle.Windsor;

namespace Composable.DependencyInjection.Windsor
{
    class WindsorDependencyInjectionContainer : IDependencyInjectionContainer, IServiceLocator
    {
        readonly IWindsorContainer _windsorContainer;
        public WindsorDependencyInjectionContainer()
        {
            _windsorContainer = new WindsorContainer();
            _windsorContainer.Kernel.Resolver.AddSubResolver(new CollectionResolver(_windsorContainer.Kernel));
        }

        public void Register(params CComponentRegistration[] registration)
        {
            var windsorRegistrations = registration.Select(ToWindsorRegistration)
                                                   .ToArray();

            _windsorContainer.Register(windsorRegistrations);
        }
        public IServiceLocator CreateServiceLocator() => this;

        public bool IsTestMode => _windsorContainer.Kernel.HasComponent(typeof(TestModeMarker));

        public IComponentLease<TComponent> Lease<TComponent>(string componentName) => new WindsorComponentLease<TComponent>(_windsorContainer.Resolve<TComponent>(componentName), _windsorContainer.Kernel);
        public IComponentLease<TComponent> Lease<TComponent>() => new WindsorComponentLease<TComponent>(_windsorContainer.Resolve<TComponent>(), _windsorContainer.Kernel);
        public IMultiComponentLease<TComponent> LeaseAll<TComponent>() => new WindsorMultiComponentLease<TComponent>(_windsorContainer.ResolveAll<TComponent>().ToArray(), _windsorContainer.Kernel);
        public IDisposable BeginScope() => _windsorContainer.BeginScope();
        public void Dispose() => _windsorContainer.Dispose();

        IRegistration ToWindsorRegistration(CComponentRegistration componentRegistration)
        {
            ComponentRegistration<object> registration = Component.For(componentRegistration.ServiceTypes);

            if (componentRegistration.InstantiationSpec.Instance != null)
            {
                registration.Instance(componentRegistration.InstantiationSpec.Instance);
            }
            else if (componentRegistration.InstantiationSpec.ImplementationType != null)
            {
                registration.ImplementedBy(componentRegistration.InstantiationSpec.ImplementationType);
            }
            else if (componentRegistration.InstantiationSpec.FactoryMethod != null)
            {
                registration.UsingFactoryMethod(kernel => componentRegistration.InstantiationSpec.FactoryMethod(new WindsorServiceLocatorKernel(kernel)));
            }
            else
            {
                throw new Exception($"Invalid {nameof(InstantiationSpec)}");
            }


            if (!componentRegistration.Name.IsNullOrEmpty())
            {
                registration = registration.Named(componentRegistration.Name);
            }

            switch (componentRegistration.Lifestyle)
            {
                case Lifestyle.Singleton:
                    return registration.LifestyleSingleton();
                case Lifestyle.Scoped:
                    return registration.LifestyleScoped();
                default:
                    throw new ArgumentOutOfRangeException(nameof(componentRegistration.Lifestyle));
            }
        }

        class WindsorServiceLocatorKernel : IServiceLocatorKernel
        {
            readonly IKernel _kernel;
            public WindsorServiceLocatorKernel(IKernel kernel) => _kernel = kernel;

            public TComponent Resolve<TComponent>() => _kernel.Resolve<TComponent>();
            public TComponent Resolve<TComponent>(string componentName) => _kernel.Resolve<TComponent>(componentName);
        }

        class WindsorComponentLease<T> : IComponentLease<T>
        {
            readonly IKernel _kernel;

            public WindsorComponentLease(T component, IKernel kernel)
            {
                _kernel = kernel;
                Instance = component;
            }

            public T Instance { get; }
            public void Dispose() => _kernel.ReleaseComponent(Instance);
        }

        class WindsorMultiComponentLease<T> : IMultiComponentLease<T>
        {
            readonly IKernel _kernel;

            public WindsorMultiComponentLease(T[] components, IKernel kernel)
            {
                _kernel = kernel;
                Instances = components;
            }

            public T[] Instances { get; }
            public void Dispose() => Instances.ForEach(instance => _kernel.ReleaseComponent(instance));
        }
    }
}