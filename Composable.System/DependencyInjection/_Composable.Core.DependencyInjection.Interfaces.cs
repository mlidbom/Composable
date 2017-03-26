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
        void Register(params CComponentRegistration[] registration);
        IServiceLocator CreateServiceLocator();
        bool IsTestMode { get; }
    }

    class TestModeMarker
    {}

    ///<summary></summary>
    public interface IServiceLocator : IDisposable
    {
        IComponentLease<TComponent> Lease<TComponent>(string componentName);
        IComponentLease<TComponent> Lease<TComponent>();
        IMultiComponentLease<TComponent> LeaseAll<TComponent>();
        IDisposable BeginScope();
    }

    public static class ServiceLocator
    {
        public static TComponent Resolve<TComponent>(this IServiceLocator @this) => @this.Lease<TComponent>()
                                                                                         .Instance;

        internal static TComponent Resolve<TComponent>(this IServiceLocator @this, string componentName) => @this.Lease<TComponent>(componentName)
                                                                                         .Instance;

        internal static TComponent[] ResolveAll<TComponent>(this IServiceLocator @this) => @this.LeaseAll<TComponent>()
                                                                                         .Instances;

        internal static void Use<TComponent>(this IServiceLocator @this, string componentName, Action<TComponent> useComponent)
        {
            using (var lease = @this.Lease<TComponent>(componentName))
            {
                useComponent(lease.Instance);
            }
        }

        public static void Use<TComponent>(this IServiceLocator @this, Action<TComponent> useComponent)
        {
            using (var lease = @this.Lease<TComponent>())
            {
                useComponent(lease.Instance);
            }
        }

        public static TResult Use<TComponent, TResult>(this IServiceLocator @this, Func<TComponent, TResult> useComponent)
        {
            using (var lease = @this.Lease<TComponent>())
            {
                return useComponent(lease.Instance);
            }
        }

        internal static void UseAll<TComponent>(this IServiceLocator @this, Action<TComponent[]> useComponent)
        {
            using (var lease = @this.LeaseAll<TComponent>())
            {
                useComponent(lease.Instances);
            }
        }

        public static TResult UseAll<TComponent, TResult>(this IServiceLocator @this, Func<TComponent[], TResult> useComponent)
        {
            using (var lease = @this.LeaseAll<TComponent>())
            {
                return useComponent(lease.Instances);
            }
        }
    }

    public static class CComponent
    {
        internal static ComponentRegistrationBuilderInitial<TService1> For<TService1, TService2, TService3>()
            => For<TService1>(Seq.OfTypes<TService2, TService3>());

        internal static ComponentRegistrationBuilderInitial<TService1> For<TService1, TService2>()
            => For<TService1>(Seq.OfTypes<TService2>());

        public static ComponentRegistrationBuilderInitial<TService> For<TService>()
            => For<TService>(new List<Type>());

        internal static ComponentRegistrationBuilderInitial<TService> For<TService>(IEnumerable<Type> additionalServices)
            => new ComponentRegistrationBuilderInitial<TService>(additionalServices);

        public class ComponentRegistrationBuilderInitialBase
        {
            protected IEnumerable<Type> ServiceTypes { get; }
            protected ComponentRegistrationBuilderInitialBase(IEnumerable<Type> serviceTypes) => ServiceTypes = serviceTypes;
        }

        public class ComponentRegistrationBuilderInitial<TService> : ComponentRegistrationBuilderInitialBase
        {
            internal ComponentRegistrationBuilderInitial(IEnumerable<Type> serviceTypes) : base(serviceTypes.Concat(new List<Type>() {typeof(TService)})) {}

            public ComponentRegistrationBuilderWithInstantiationSpec<TService> ImplementedBy<TImplementation>()
            {
                Contract.Arguments.That(ServiceTypes.All(serviceType => serviceType.IsAssignableFrom(typeof(TImplementation))), "The implementing type must implement all the service interfaces.");
                return new ComponentRegistrationBuilderWithInstantiationSpec<TService>(ServiceTypes, InstantiationSpec.ImplementedBy(typeof(TImplementation)));
            }

            internal ComponentRegistrationBuilderWithInstantiationSpec<TService> Instance(TService instance)
            {
                Contract.Arguments.That(ServiceTypes.All(serviceType => serviceType.IsInstanceOfType(instance)), "The implementing type must implement all the service interfaces.");
                return new ComponentRegistrationBuilderWithInstantiationSpec<TService>(ServiceTypes, InstantiationSpec.FromInstance(instance));
            }

            internal ComponentRegistrationBuilderWithInstantiationSpec<TService> UsingFactoryMethod<TImplementation>(Func<IServiceLocator, TImplementation> factoryMethod)
                where TImplementation : TService
            {
                return new ComponentRegistrationBuilderWithInstantiationSpec<TService>(ServiceTypes, InstantiationSpec.FromFactoryMethod(serviceLocator => factoryMethod(serviceLocator)));
            }
        }

        public class ComponentRegistrationBuilderWithInstantiationSpec<TService>
        {
            readonly IEnumerable<Type> _serviceTypes;
            readonly InstantiationSpec _instantInstatiationSpec;
            string Name { get; set; }

            internal ComponentRegistrationBuilderWithInstantiationSpec(IEnumerable<Type> serviceTypes, InstantiationSpec instantInstatiationSpec)
            {
                _serviceTypes = serviceTypes;
                _instantInstatiationSpec = instantInstatiationSpec;
            }

            public ComponentRegistrationBuilderWithInstantiationSpec<TService> Named(string name)
            {
                Contract.Arguments.That(Name == null, "Name == null");
                Name = name;
                return this;
            }

            internal CComponentRegistration LifestyleSingleton() => new CComponentRegistration(Lifestyle.Singleton, Name, _serviceTypes, _instantInstatiationSpec);
            public CComponentRegistration LifestyleScoped() => new CComponentRegistration(Lifestyle.Scoped, Name, _serviceTypes, _instantInstatiationSpec);

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
        internal Type ImplementationType { get; }
        internal Func<IServiceLocator, object> FactoryMethod { get; }

        internal static InstantiationSpec FromInstance(object instance) => new InstantiationSpec(instance);

        internal static InstantiationSpec ImplementedBy(Type implementationType) => new InstantiationSpec(implementationType);

        internal static InstantiationSpec FromFactoryMethod(Func<IServiceLocator, object> factoryMethod) => new InstantiationSpec(factoryMethod);

        InstantiationSpec(Type implementationType) => ImplementationType = implementationType;

        InstantiationSpec(Func<IServiceLocator, object> factoryMethod) => FactoryMethod = factoryMethod;

        InstantiationSpec(object instance) => Instance = instance;
    }

    public class CComponentRegistration
    {
        internal IEnumerable<Type> ServiceTypes { get; }
        internal InstantiationSpec InstantiationSpec { get; }
        internal Lifestyle Lifestyle { get; }
        internal string Name { get; }
        internal CComponentRegistration(Lifestyle lifestyle, string name, IEnumerable<Type> serviceTypes, InstantiationSpec instantiationSpec)
        {
            serviceTypes = serviceTypes.ToList();

            Contract.Arguments.That(lifestyle == Lifestyle.Singleton || instantiationSpec.Instance == null, $"{nameof(InstantiationSpec.Instance)} registrations must be {nameof(Lifestyle.Singleton)}s");

            Name = name;
            ServiceTypes = serviceTypes;
            InstantiationSpec = instantiationSpec;
            Lifestyle = lifestyle;
        }
    }
}
