using System;
using System.Collections.Generic;
using System.Linq;
using SimpleInjector;
using SimpleInjector.Lifestyles;

namespace Composable.DependencyInjection.SimpleInjectorImplementation
{
    public class SimpleInjectorDependencyInjectionContainer : IDependencyInjectionContainer, IServiceLocator, IServiceLocatorKernel
    {
        readonly Container _container;
        readonly List<ComponentRegistration> _registeredComponents = new List<ComponentRegistration>();
        internal SimpleInjectorDependencyInjectionContainer() => _container = new Container();

        public void Register(params ComponentRegistration[] registrations)
        {
            _registeredComponents.AddRange(registrations);

            foreach(var componentRegistration in registrations)
            {
                SimpleInjector.Lifestyle lifestyle = null;
                switch (componentRegistration.Lifestyle)
                {
                    case Lifestyle.Singleton:
                        lifestyle = SimpleInjector.Lifestyle.Singleton;
                        break;
                    case Lifestyle.Scoped:
                        lifestyle = SimpleInjector.Lifestyle.Scoped;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(componentRegistration.Lifestyle));
                }

                if (componentRegistration.InstantiationSpec.Instance != null)
                {
                    foreach(var serviceType in componentRegistration.ServiceTypes)
                    {
                        _container.Register(serviceType, () => componentRegistration.InstantiationSpec.Instance, lifestyle);
                    }
                } else if(componentRegistration.InstantiationSpec.ImplementationType != null)
                {
                    foreach (var serviceType in componentRegistration.ServiceTypes)
                    {
                        _container.Register(serviceType, componentRegistration.InstantiationSpec.ImplementationType, lifestyle);
                    }
                } else if(componentRegistration.InstantiationSpec.FactoryMethod != null)
                {
                    foreach (var serviceType in componentRegistration.ServiceTypes)
                    {
                        _container.Register(serviceType, () => componentRegistration.InstantiationSpec.FactoryMethod(this), lifestyle);
                    }
                } else
                {
                    throw new Exception($"Invalid {nameof(InstantiationSpec)}");
                }
            }
        }
        public IEnumerable<ComponentRegistration> RegisteredComponents() => _registeredComponents;

        IServiceLocator IDependencyInjectionContainer.CreateServiceLocator() => this;

        IComponentLease<TComponent> IServiceLocator.Lease<TComponent>() => new SimpleInjectorComponentLease<TComponent>(_container.GetInstance<TComponent>());
        IMultiComponentLease<TComponent> IServiceLocator.LeaseAll<TComponent>() => new SimpleInjectorMultiComponentLease<TComponent>(_container.GetAllInstances<TComponent>().ToArray());
        IDisposable IServiceLocator.BeginScope() => AsyncScopedLifestyle.BeginScope(_container);
        void IDisposable.Dispose() => _container.Dispose();

        sealed class SimpleInjectorComponentLease<T> : IComponentLease<T>
        {
            readonly T _instance;

            internal SimpleInjectorComponentLease(T component) => _instance = component;

            T IComponentLease<T>.Instance => _instance;
            void IDisposable.Dispose() {}
        }

        sealed class SimpleInjectorMultiComponentLease<T> : IMultiComponentLease<T>
        {
            readonly T[] _instances;

            internal SimpleInjectorMultiComponentLease(T[] components) => _instances = components;

            T[] IMultiComponentLease<T>.Instances => _instances;
            void IDisposable.Dispose() {}
        }

        TComponent IServiceLocatorKernel.Resolve<TComponent>() => _container.GetInstance<TComponent>();
    }
}
