using System;
using System.Collections.Generic;
using System.Linq;
using Composable.Contracts;
using Composable.System;
using Composable.System.Linq;
using JetBrains.Annotations;

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
        void Register(params ComponentRegistration[] registration);
        IServiceLocator CreateServiceLocator();
        bool IsTestMode { get; }
    }

    class TestModeMarker
    {}

    ///<summary></summary>
    public interface IServiceLocator : IDisposable
    {
        IComponentLease<TComponent> Lease<TComponent>();
        IMultiComponentLease<TComponent> LeaseAll<TComponent>();
        IDisposable BeginScope();
    }

    public interface IServiceLocatorKernel
    {
        TComponent Resolve<TComponent>();
    }

    public static class ServiceLocator
    {
        internal static TComponent Resolve<TComponent>(this IServiceLocator @this) => @this.Lease<TComponent>()
                                                                                         .Instance;

        internal static TComponent[] ResolveAll<TComponent>(this IServiceLocator @this) => @this.LeaseAll<TComponent>()
                                                                                         .Instances;

        public static void Use<TComponent>(this IServiceLocator @this,[InstantHandle] Action<TComponent> useComponent)
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

        internal static void UseAll<TComponent>(this IServiceLocator @this, [InstantHandle] Action<TComponent[]> useComponent)
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

    public static class Component
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

            public ComponentRegistrationBuilderWithInstantiationSpec ImplementedBy<TImplementation>()
            {
                Contract.Arguments.That(ServiceTypes.All(serviceType => serviceType.IsAssignableFrom(typeof(TImplementation))), "The implementing type must implement all the service interfaces.");
                return new ComponentRegistrationBuilderWithInstantiationSpec(ServiceTypes, InstantiationSpec.ImplementedBy(typeof(TImplementation)));
            }

            internal ComponentRegistrationBuilderWithInstantiationSpec Instance(TService instance)
            {
                Contract.Arguments.That(ServiceTypes.All(serviceType => serviceType.IsInstanceOfType(instance)), "The implementing type must implement all the service interfaces.");
                return new ComponentRegistrationBuilderWithInstantiationSpec(ServiceTypes, InstantiationSpec.FromInstance(instance));
            }

            internal ComponentRegistrationBuilderWithInstantiationSpec UsingFactoryMethod<TImplementation>(Func<IServiceLocatorKernel, TImplementation> factoryMethod)
                where TImplementation : TService
            {
                return new ComponentRegistrationBuilderWithInstantiationSpec(ServiceTypes, InstantiationSpec.FromFactoryMethod(serviceLocator => factoryMethod(serviceLocator)));
            }
        }

        public class ComponentRegistrationBuilderWithInstantiationSpec
        {
            readonly IEnumerable<Type> _serviceTypes;
            readonly InstantiationSpec _instantInstatiationSpec;

            internal ComponentRegistrationBuilderWithInstantiationSpec(IEnumerable<Type> serviceTypes, InstantiationSpec instantInstatiationSpec)
            {
                _serviceTypes = serviceTypes;
                _instantInstatiationSpec = instantInstatiationSpec;
            }

            internal ComponentRegistration LifestyleSingleton() => new ComponentRegistration(Lifestyle.Singleton, _serviceTypes, _instantInstatiationSpec);
            public ComponentRegistration LifestyleScoped() => new ComponentRegistration(Lifestyle.Scoped, _serviceTypes, _instantInstatiationSpec);

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
        internal Func<IServiceLocatorKernel, object> FactoryMethod { get; }

        internal static InstantiationSpec FromInstance(object instance) => new InstantiationSpec(instance);

        internal static InstantiationSpec ImplementedBy(Type implementationType) => new InstantiationSpec(implementationType);

        internal static InstantiationSpec FromFactoryMethod(Func<IServiceLocatorKernel, object> factoryMethod) => new InstantiationSpec(factoryMethod);

        InstantiationSpec(Type implementationType) => ImplementationType = implementationType;

        InstantiationSpec(Func<IServiceLocatorKernel, object> factoryMethod) => FactoryMethod = factoryMethod;

        InstantiationSpec(object instance) => Instance = instance;
    }

    public class ComponentRegistration
    {
        internal IEnumerable<Type> ServiceTypes { get; }
        internal InstantiationSpec InstantiationSpec { get; }
        internal Lifestyle Lifestyle { get; }
        internal ComponentRegistration(Lifestyle lifestyle, IEnumerable<Type> serviceTypes, InstantiationSpec instantiationSpec)
        {
            serviceTypes = serviceTypes.ToList();

            Contract.Arguments.That(lifestyle == Lifestyle.Singleton || instantiationSpec.Instance == null, $"{nameof(InstantiationSpec.Instance)} registrations must be {nameof(Lifestyle.Singleton)}s");

            ServiceTypes = serviceTypes;
            InstantiationSpec = instantiationSpec;
            Lifestyle = lifestyle;
        }
    }
}
