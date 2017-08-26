using System;
using System.Collections.Generic;
using System.Linq;
using Composable.Contracts;
using SimpleInjector;
using SimpleInjector.Lifestyles;

namespace Composable.DependencyInjection.SimpleInjectorImplementation
{
    public class SimpleInjectorDependencyInjectionContainer : IDependencyInjectionContainer, IServiceLocator, IServiceLocatorKernel
    {
        readonly Container _container;
        readonly List<ComponentRegistration> _registeredComponents = new List<ComponentRegistration>();
        internal SimpleInjectorDependencyInjectionContainer(IRunMode runMode)
        {
            RunMode = runMode;
            _container = new Container();
            _container.Options.DefaultScopedLifestyle = new AsyncScopedLifestyle();
        }

        public IRunMode RunMode { get; }
        public void Register(params ComponentRegistration[] registrations)
        {
            _registeredComponents.AddRange(registrations);

            foreach(var componentRegistration in registrations)
            {
                SimpleInjector.Lifestyle lifestyle;
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
                    Contract.Assert.That(lifestyle == SimpleInjector.Lifestyle.Singleton, "Instance can only be used with singletons.");
                    foreach(var serviceType in componentRegistration.ServiceTypes)
                    {
                        _container.RegisterSingleton(serviceType, componentRegistration.InstantiationSpec.Instance);
                    }
                } else if(componentRegistration.InstantiationSpec.ImplementationType != null)
                {
                    foreach (var serviceType in componentRegistration.ServiceTypes)
                    {
                        _container.Register(serviceType, componentRegistration.InstantiationSpec.ImplementationType, lifestyle);
                    }
                } else if(componentRegistration.InstantiationSpec.FactoryMethod != null)
                {
                    foreach (var someCompletelyOtherName in componentRegistration.ServiceTypes)
                    {
                        _container.Register(someCompletelyOtherName, () => componentRegistration.InstantiationSpec.FactoryMethod(this), lifestyle);
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
