using System;
using System.Collections.Generic;
using System.Linq;
using Composable.Contracts;
using Composable.System;
using Composable.System.Linq;

namespace Composable.DependencyInjection
{
    ///<summary>A lease to use a component.
    /// <para>Should be disposed as soon as the component is no longer in use.</para>
    /// <para>An exception is thrown if dispose fails to be called. </para>
    /// <para>should inherit from <see cref="StrictlyManagedResourceBase{TInheritor}"/> or have a member field of type: <see cref="StrictlyManagedResource{TManagedResource}"/></para>
    /// </summary>
    interface IComponentLease<out TComponent> : IDisposable
    {
        TComponent Instance { get; }
    }

    interface IDependencyInjectionContainer
    {
        IDependencyInjectionContainer Register(params CComponentRegistration[] registration);
    }

    static class CComponent
    {
        internal static ComponentRegistrationBuilderInitial<TService1> For<TService1, TService2, TService3>()
            => For<TService1>(Seq.OfTypes<TService2, TService3>());

        internal static ComponentRegistrationBuilderInitial<TService1> For<TService1, TService2>()
            => For<TService1>(Seq.OfTypes<TService2>());

        internal static ComponentRegistrationBuilderInitial<TService> For<TService>()
            => For<TService>(new List<Type>());

        internal static ComponentRegistrationBuilderInitial<TService> For<TService>(IEnumerable<Type> additionalServices)
            => new ComponentRegistrationBuilderInitial<TService>(additionalServices);

        internal class ComponentRegistrationBuilderInitialBase
        {
            protected IEnumerable<Type> ServiceTypes { get; }
            protected ComponentRegistrationBuilderInitialBase(IEnumerable<Type> serviceTypes) => ServiceTypes = serviceTypes;
        }

        internal class ComponentRegistrationBuilderInitial<TService> : ComponentRegistrationBuilderInitialBase
        {
            public ComponentRegistrationBuilderInitial(IEnumerable<Type> serviceTypes) : base(serviceTypes.Concat(new List<Type>() {typeof(TService)})) {}

            public ComponentRegistrationBuilderWithInstantiationSpec<TService> ImplementedBy<TImplementation>()
            {
                Contract.Arguments.That(ServiceTypes.All(serviceType => serviceType.IsAssignableFrom(typeof(TImplementation))), "The implementing type must implement all the service interfaces.");
                return new ComponentRegistrationBuilderWithInstantiationSpec<TService>(ServiceTypes, InstantiationSpec.ImplementedBy(typeof(TImplementation)));
            }

            public ComponentRegistrationBuilderWithInstantiationSpec<TService> Instance(TService instance)
            {
                Contract.Arguments.That(ServiceTypes.All(serviceType => serviceType.IsInstanceOfType(instance)), "The implementing type must implement all the service interfaces.");
                return new ComponentRegistrationBuilderWithInstantiationSpec<TService>(ServiceTypes, InstantiationSpec.FromInstance(instance));
            }
        }

        internal class ComponentRegistrationBuilderWithInstantiationSpec<TService>
        {
            readonly IEnumerable<Type> _serviceTypes;
            readonly InstantiationSpec _instantInstatiationSpec;
            public string Name { get; private set; }

            public ComponentRegistrationBuilderWithInstantiationSpec(IEnumerable<Type> serviceTypes, InstantiationSpec instantInstatiationSpec)
            {
                _serviceTypes = serviceTypes;
                _instantInstatiationSpec = instantInstatiationSpec;
            }


            ComponentRegistrationBuilderWithInstantiationSpec<TService> Named(string name)
            {
                Contract.Arguments.That(Name == null, "Name == null");
                Name = name;
                return this;
            }

            public CComponentRegistration LifestyleSingleton() => new CComponentRegistration(Lifestyle.Singleton, Name, _serviceTypes, _instantInstatiationSpec);
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

        internal static InstantiationSpec FromInstance(object instance) => new InstantiationSpec(instance);

        internal static InstantiationSpec ImplementedBy(Type implementationType) => new InstantiationSpec(implementationType);

        InstantiationSpec(Type implementationType) => ImplementationType = implementationType;

        InstantiationSpec(object instance) => Instance = instance;
    }

    class CComponentRegistration
    {
        public IEnumerable<Type> ServiceTypes { get; }
        internal InstantiationSpec InstantiationSpec { get; }
        internal Lifestyle Lifestyle { get; }
        internal string Name { get; }
        public CComponentRegistration(Lifestyle lifestyle, string name, IEnumerable<Type> serviceTypes, InstantiationSpec instantiationSpec)
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
