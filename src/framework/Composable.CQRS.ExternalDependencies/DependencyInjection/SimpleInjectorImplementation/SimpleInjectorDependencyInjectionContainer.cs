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

            _container.ResolveUnregisteredType += (sender, unregisteredTypeEventArgs) =>
            {
                if (!unregisteredTypeEventArgs.Handled && !unregisteredTypeEventArgs.UnregisteredServiceType.IsAbstract)
                {
                    throw new InvalidOperationException(unregisteredTypeEventArgs.UnregisteredServiceType.ToFriendlyName() + " has not been registered.");
                }
            };
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
                    var baseRegistration = GetSimpleInjectorLifestyle(componentRegistration.Lifestyle).CreateRegistration(componentRegistration.InstantiationSpec.ImplementationType, _container);

                    foreach (var serviceType in componentRegistration.ServiceTypes)
                    {
                        _container.AddRegistration(serviceType, baseRegistration);
                    }

                } else if(componentRegistration.InstantiationSpec.FactoryMethod != null)
                {
                    var baseRegistration = GetSimpleInjectorLifestyle(componentRegistration.Lifestyle)
                       .CreateRegistration(
                            componentRegistration.InstantiationSpec.FactoryMethodReturnType,
                            () => componentRegistration.InstantiationSpec.FactoryMethod(this),
                            _container);
                    foreach (var someCompletelyOtherName in componentRegistration.ServiceTypes)
                    {
                        _container.AddRegistration(someCompletelyOtherName, baseRegistration);
                    }
                } else
                {
                    throw new Exception($"Invalid {nameof(InstantiationSpec)}");
                }
            }
        }


        SimpleInjector.Lifestyle GetSimpleInjectorLifestyle(Lifestyle @this)
        {
            switch(@this)
            {
                case Lifestyle.Singleton:
                    return SimpleInjector.Lifestyle.Singleton;
                case Lifestyle.Scoped:
                    return SimpleInjector.Lifestyle.Scoped;
                default:
                    throw new ArgumentOutOfRangeException(nameof(@this), @this, null);
            }
        }

        public IEnumerable<ComponentRegistration> RegisteredComponents() => _registeredComponents;

        bool _verified;

        IServiceLocator IDependencyInjectionContainer.CreateServiceLocator()
        {
            if(!_verified)
            {
                _verified = true;
                _container.Verify();
            }
            return this;
        }

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
