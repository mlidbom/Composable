using System;
using System.Collections.Generic;
using System.Linq;
using Composable.Contracts;
using Composable.System;
using Composable.System.Linq;

// ReSharper disable UnusedMember.Global

namespace Composable.DependencyInjection
{
    ///<summary>A lease to use a component.
    /// <para>Should be disposed as soon as the component is no longer in use.</para>
    /// <para>An exception is thrown if dispose fails to be called. </para>
    /// <para>should inherit from <see cref="StrictlyManagedResourceBase{TInheritor}"/> or have a member field of type: <see cref="StrictlyManagedResource{TManagedResource}"/></para>
    /// </summary>
    public interface IComponentLease<out TComponent> : IDisposable
    {
        TComponent Instance { get; }
    }

    public interface IMultiComponentLease<out TComponent> : IDisposable
    {
        TComponent[] Instances { get; }
    }

    public interface IDependencyInjectionContainer : IDisposable
    {
        IRunMode RunMode { get; }
        void Register(params ComponentRegistration[] registrations);
        IEnumerable<ComponentRegistration> RegisteredComponents();
        IServiceLocator CreateServiceLocator();
    }

    public interface IRunMode
    {
        bool IsTesting { get; }
        TestingMode TestingMode { get; }
    }

    class RunMode : IRunMode
    {
        readonly bool _isTesting;
        bool IRunMode.IsTesting => _isTesting;
        public TestingMode TestingMode { get; }

        public static readonly IRunMode Production = new RunMode(isTesting:false, testingMode: TestingMode.DatabasePool);

        public RunMode(bool isTesting, TestingMode testingMode)
        {
            TestingMode = testingMode;
            _isTesting = isTesting;
        }
    }

    ///<summary></summary>
    public interface IServiceLocator : IDisposable
    {
        TComponent Resolve<TComponent>() where TComponent : class;
        TComponent[] ResolveAll<TComponent>() where TComponent : class;
        IDisposable BeginScope();
    }

    interface IServiceLocatorKernel
    {
        TComponent Resolve<TComponent>() where TComponent : class;
    }

    public static class Singleton
    {
        public static Component.ComponentRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3, TService4, TService5, TService6, TService7, TService8, TService9>() where TService1 : class => For<TService1>(Seq.OfTypes<TService2, TService3, TService4, TService5, TService6, TService7, TService8, TService9>());
        public static Component.ComponentRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3, TService4, TService5, TService6, TService7, TService8>() where TService1 : class => For<TService1>(Seq.OfTypes<TService2, TService3, TService4, TService5, TService6, TService7, TService8>());
        public static Component.ComponentRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3, TService4, TService5, TService6, TService7>() where TService1 : class => For<TService1>(Seq.OfTypes<TService2, TService3, TService4, TService5, TService6, TService7>());
        public static Component.ComponentRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3, TService4, TService5, TService6>() where TService1 : class => For<TService1>(Seq.OfTypes<TService2, TService3, TService4, TService5, TService6>());
        public static Component.ComponentRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3, TService4, TService5>() where TService1 : class => For<TService1>(Seq.OfTypes<TService2, TService3, TService4, TService5>());
        public static Component.ComponentRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3, TService4>() where TService1 : class => For<TService1>(Seq.OfTypes<TService2, TService3, TService4>());
        public static Component.ComponentRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3>() where TService1 : class => For<TService1>(Seq.OfTypes<TService2, TService3>());
        public static Component.ComponentRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2>() where TService1 : class => For<TService1>(Seq.OfTypes<TService2>());
        public static Component.ComponentRegistrationWithoutInstantiationSpec<TService> For<TService>() where TService : class => For<TService>(new List<Type>());
        static Component.ComponentRegistrationWithoutInstantiationSpec<TService> For<TService>(IEnumerable<Type> additionalServices) where TService : class => new Component.ComponentRegistrationWithoutInstantiationSpec<TService>(Lifestyle.Singleton, additionalServices);
    }

    public static class Scoped
    {
        public static Component.ComponentRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3, TService4, TService5, TService6, TService7, TService8, TService9>() where TService1 : class => For<TService1>(Seq.OfTypes<TService2, TService3, TService4, TService5, TService6, TService7, TService8, TService9>());
        public static Component.ComponentRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3, TService4, TService5, TService6, TService7, TService8>() where TService1 : class => For<TService1>(Seq.OfTypes<TService2, TService3, TService4, TService5, TService6, TService7, TService8>());
        public static Component.ComponentRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3, TService4, TService5, TService6, TService7>() where TService1 : class => For<TService1>(Seq.OfTypes<TService2, TService3, TService4, TService5, TService6, TService7>());
        public static Component.ComponentRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3, TService4, TService5, TService6>() where TService1 : class => For<TService1>(Seq.OfTypes<TService2, TService3, TService4, TService5, TService6>());
        public static Component.ComponentRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3, TService4, TService5>() where TService1 : class => For<TService1>(Seq.OfTypes<TService2, TService3, TService4, TService5>());
        public static Component.ComponentRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3, TService4>() where TService1 : class => For<TService1>(Seq.OfTypes<TService2, TService3, TService4>());
        public static Component.ComponentRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3>() where TService1 : class => For<TService1>(Seq.OfTypes<TService2, TService3>());
        public static Component.ComponentRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2>() where TService1 : class => For<TService1>(Seq.OfTypes<TService2>());
        public static Component.ComponentRegistrationWithoutInstantiationSpec<TService> For<TService>() where TService : class => For<TService>(new List<Type>());
        static Component.ComponentRegistrationWithoutInstantiationSpec<TService> For<TService>(IEnumerable<Type> additionalServices) where TService : class => new Component.ComponentRegistrationWithoutInstantiationSpec<TService>(Lifestyle.Scoped, additionalServices);
    }

    public static class Component
    {
        public static ComponentRegistrationWithoutLifestyleAndInstantiationSpec<TService1> For<TService1, TService2, TService3, TService4, TService5, TService6, TService7, TService8, TService9>() where TService1 : class => For<TService1>(Seq.OfTypes<TService2, TService3, TService4, TService5, TService6, TService7, TService8, TService9>());
        public static ComponentRegistrationWithoutLifestyleAndInstantiationSpec<TService1> For<TService1, TService2, TService3, TService4, TService5, TService6, TService7, TService8>() where TService1 : class => For<TService1>(Seq.OfTypes<TService2, TService3, TService4, TService5, TService6, TService7, TService8>());
        public static ComponentRegistrationWithoutLifestyleAndInstantiationSpec<TService1> For<TService1, TService2, TService3, TService4, TService5, TService6, TService7>() where TService1 : class => For<TService1>(Seq.OfTypes<TService2, TService3, TService4, TService5, TService6, TService7>());
        public static ComponentRegistrationWithoutLifestyleAndInstantiationSpec<TService1> For<TService1, TService2, TService3, TService4, TService5, TService6>() where TService1 : class => For<TService1>(Seq.OfTypes<TService2, TService3, TService4, TService5, TService6>());
        public static ComponentRegistrationWithoutLifestyleAndInstantiationSpec<TService1> For<TService1, TService2, TService3, TService4, TService5>() where TService1 : class => For<TService1>(Seq.OfTypes<TService2, TService3, TService4, TService5>());
        public static ComponentRegistrationWithoutLifestyleAndInstantiationSpec<TService1> For<TService1, TService2, TService3, TService4>() where TService1 : class => For<TService1>(Seq.OfTypes<TService2, TService3, TService4>());
        public static ComponentRegistrationWithoutLifestyleAndInstantiationSpec<TService1> For<TService1, TService2, TService3>() where TService1 : class => For<TService1>(Seq.OfTypes<TService2, TService3>());
        public static ComponentRegistrationWithoutLifestyleAndInstantiationSpec<TService1> For<TService1, TService2>() where TService1 : class => For<TService1>(Seq.OfTypes<TService2>());
        public static ComponentRegistrationWithoutLifestyleAndInstantiationSpec<TService> For<TService>() where TService : class => For<TService>(new List<Type>());
        internal static ComponentRegistrationWithoutLifestyleAndInstantiationSpec<TService> For<TService>(IEnumerable<Type> additionalServices) where TService : class => new ComponentRegistrationWithoutLifestyleAndInstantiationSpec<TService>(additionalServices);

        public class ComponentRegistrationBuilderInitialBase
        {
            protected IEnumerable<Type> ServiceTypes { get; }
            protected ComponentRegistrationBuilderInitialBase(IEnumerable<Type> serviceTypes) => ServiceTypes = serviceTypes;
        }

        public class ComponentRegistrationWithoutLifestyleAndInstantiationSpec<TService> : ComponentRegistrationBuilderInitialBase where TService : class
        {
            internal ComponentRegistrationWithoutLifestyleAndInstantiationSpec(IEnumerable<Type> serviceTypes) : base(serviceTypes.Concat(new List<Type>() {typeof(TService)})) {}

            internal ComponentRegistrationBuilderWithInstantiationSpec<TService> UsingFactoryMethod<TImplementation>(Func<IServiceLocatorKernel, TImplementation> factoryMethod)
                where TImplementation : TService
            {
                var implementationType = typeof(TImplementation);
                AssertImplementsAllServices(implementationType);
                return new ComponentRegistrationBuilderWithInstantiationSpec<TService>(ServiceTypes,
                                                                                       InstantiationSpec.FromFactoryMethod(serviceLocator => factoryMethod(serviceLocator), implementationType));
            }

            internal ComponentRegistrationBuilderWithInstantiationSpec<TService> UsingFactoryMethod(Type implementationType, Func<IServiceLocatorKernel, object> factoryMethod)
            {
                AssertImplementsAllServices(implementationType);
                return new ComponentRegistrationBuilderWithInstantiationSpec<TService>(ServiceTypes,
                                                                                       InstantiationSpec.FromFactoryMethod(factoryMethod, implementationType));
            }

            void AssertImplementsAllServices(Type implementationType)
            {
                var unImplementedService = ServiceTypes.FirstOrDefault(serviceType => !serviceType.IsAssignableFrom(implementationType));
                if(unImplementedService != null)
                {
                    throw new ArgumentException($"{implementationType.FullName} does not implement: {unImplementedService.FullName}");
                }
            }

        }

        public class ComponentRegistrationWithoutInstantiationSpec<TService> : ComponentRegistrationBuilderInitialBase where TService : class
        {
            readonly Lifestyle _lifestyle;
            internal ComponentRegistrationWithoutInstantiationSpec(Lifestyle lifestyle, IEnumerable<Type> serviceTypes) : base(serviceTypes.Concat(new List<Type>() {typeof(TService)})) { _lifestyle = lifestyle; }

            internal ComponentRegistration<TService> UsingFactoryMethod<TImplementation>(Func<IServiceLocatorKernel, TImplementation> factoryMethod)
                where TImplementation : TService
            {
                var implementationType = typeof(TImplementation);
                AssertImplementsAllServices(implementationType);
                return new ComponentRegistration<TService>(_lifestyle, ServiceTypes, InstantiationSpec.FromFactoryMethod(serviceLocator => factoryMethod(serviceLocator), implementationType));
            }

            internal ComponentRegistration<TService> UsingFactoryMethod(Type implementationType, Func<IServiceLocatorKernel, object> factoryMethod)
            {
                AssertImplementsAllServices(implementationType);
                return new ComponentRegistration<TService>(_lifestyle, ServiceTypes, InstantiationSpec.FromFactoryMethod(factoryMethod, implementationType));
            }

            void AssertImplementsAllServices(Type implementationType)
            {
                var unImplementedService = ServiceTypes.FirstOrDefault(serviceType => !serviceType.IsAssignableFrom(implementationType));
                if(unImplementedService != null)
                {
                    throw new ArgumentException($"{implementationType.FullName} does not implement: {unImplementedService.FullName}");
                }
            }
        }

        public class ComponentRegistrationBuilderWithInstantiationSpec<TService> where TService : class
        {
            readonly IEnumerable<Type> _serviceTypes;
            readonly InstantiationSpec _instantiationSpec;

            internal ComponentRegistrationBuilderWithInstantiationSpec(IEnumerable<Type> serviceTypes, InstantiationSpec instantiationSpec)
            {
                _serviceTypes = serviceTypes;
                _instantiationSpec = instantiationSpec;
            }

            public ComponentRegistration<TService> LifestyleSingleton() => new ComponentRegistration<TService>(Lifestyle.Singleton, _serviceTypes, _instantiationSpec);
            public ComponentRegistration<TService> LifestyleScoped() => new ComponentRegistration<TService>(Lifestyle.Scoped, _serviceTypes, _instantiationSpec);

        }
    }

    enum Lifestyle
    {
        Singleton,
        Scoped
    }

    class InstantiationSpec
    {
        internal object Instance { get; }
        internal object RunFactoryMethod(IServiceLocatorKernel kern) => FactoryMethod(kern);
        internal Func<IServiceLocatorKernel, object> FactoryMethod { get; }
        internal Type FactoryMethodReturnType { get; }

        internal static InstantiationSpec FromInstance(object instance) => new InstantiationSpec(instance);

        internal static InstantiationSpec FromFactoryMethod(Func<IServiceLocatorKernel, object> factoryMethod, Type factoryMethodReturnType) => new InstantiationSpec(factoryMethod, factoryMethodReturnType);

        InstantiationSpec(Func<IServiceLocatorKernel, object> factoryMethod, Type factoryMethodReturnType)
        {
            FactoryMethod = factoryMethod;
            FactoryMethodReturnType = factoryMethodReturnType;
        }

        InstantiationSpec(object instance)
        {
            Instance = instance;
            FactoryMethod = kern => instance;
        }
    }

    public abstract class ComponentRegistration
    {
        internal Guid Id { get; } = Guid.NewGuid();
        internal IEnumerable<Type> ServiceTypes { get; }
        internal InstantiationSpec InstantiationSpec { get; }
        internal Lifestyle Lifestyle { get; }
        internal abstract int ComponentIndex {get;}

        internal readonly int[] ServiceTypeIndexes;

        readonly object _lock = new object();
        object _singletonInstance;

        internal object CreateInstance(IServiceLocatorKernel kernel) => InstantiationSpec.FactoryMethod(kernel);

        internal object GetSingletonInstance(IServiceLocatorKernel kernel)
        {
            lock(_lock)
            {
                if(_singletonInstance == null)
                {
                    _singletonInstance = CreateInstance(kernel);
                }
            }

            return _singletonInstance;
        }

        internal ComponentRegistration(Lifestyle lifestyle, IEnumerable<Type> serviceTypes, InstantiationSpec instantiationSpec)
        {
            serviceTypes = serviceTypes.ToList();

            ServiceTypeIndexes = serviceTypes.Select(ComposableDependencyInjectionContainer.ServiceTypeIndex.For).ToArray();

            Contract.Arguments.That(lifestyle == Lifestyle.Singleton || instantiationSpec.Instance == null, $"{nameof(InstantiationSpec.Instance)} registrations must be {nameof(Lifestyle.Singleton)}s");

            ServiceTypes = serviceTypes;
            InstantiationSpec = instantiationSpec;
            Lifestyle = lifestyle;
        }

        internal abstract ComponentRegistration CreateCloneRegistration(IServiceLocator currentLocator);

        internal void Dispose()
        {
            if(InstantiationSpec.Instance == null && _singletonInstance is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        internal abstract object Resolve(ComposableDependencyInjectionContainer serviceLocator);
    }

    public class ComponentRegistration<TService> : ComponentRegistration where TService : class
    {
        bool ShouldDelegateToParentWhenCloning { get; set; }

        internal ComponentRegistration<TService> DelegateToParentServiceLocatorWhenCloning()
        {
            Contract.Assert.That(Lifestyle == Lifestyle.Singleton, "Only singletons can be delegated to parent container since disposal concern handling becomes very confused for any other lifestyle");
            ShouldDelegateToParentWhenCloning = true;
            return this;
        }

        internal override int ComponentIndex => ComposableDependencyInjectionContainer.ServiceTypeIndex.ForService<TService>.Index;
        internal override ComponentRegistration CreateCloneRegistration(IServiceLocator currentLocator)
        {
            if(!ShouldDelegateToParentWhenCloning)
            {
                return new ComponentRegistration<TService>(Lifestyle, ServiceTypes, InstantiationSpec);
            }

            //We must use singleton instance registrations when delegating because otherwise the containers will both attempt to dispose the service.
            //Instance registrations are not disposed.
            return new ComponentRegistration<TService>(
                lifestyle: Lifestyle.Singleton,
                serviceTypes: ServiceTypes,
                instantiationSpec: InstantiationSpec.FromInstance(currentLocator.Resolve<TService>())
            );
        }

        internal override object Resolve(ComposableDependencyInjectionContainer locator) => locator.Resolve<TService>();

        internal ComponentRegistration(Lifestyle lifestyle, IEnumerable<Type> serviceTypes, InstantiationSpec instantiationSpec)
            :base(lifestyle, serviceTypes, instantiationSpec)
        {}
    }
}
