using System;
using System.Collections.Generic;
using System.Linq;
using Composable.Contracts;
using Composable.System;

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
        IDependencyInjectionContainer Register(params ComponentRegistration[] registration);
    }

    class Component
    {
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

            public ComponentRegistrationBuilderWithImplementation<TService> ImplementedBy<TImplementation>()
                => new ComponentRegistrationBuilderWithImplementation<TService>(ServiceTypes,
                                                                                typeof(TImplementation));
        }

        internal class ComponentRegistrationBuilderWithImplementation<TService>
        {
            readonly IEnumerable<Type> _serviceTypes;
            readonly Type _implementingType;
            public string Name { get; private set; }

            public ComponentRegistrationBuilderWithImplementation(IEnumerable<Type> serviceTypes, Type implementingType)
            {
                _serviceTypes = serviceTypes;
                _implementingType = implementingType;
            }


            ComponentRegistrationBuilderWithImplementation<TService> Named(string name)
            {
                Contract.Arguments.That(Name == null, "Name == null");
                Name = name;
                return this;
            }

            public ComponentRegistration LifeStyleSingleton() => new ComponentRegistration(LifeStyle.Singleton, Name, _serviceTypes, _implementingType);

        }
    }

    enum LifeStyle
    {
        Singleton,
        Scoped
    }

    class ComponentRegistration
    {
        public IEnumerable<Type> ServiceTypes { get; }
        internal Type ImplementingType { get; }
        internal LifeStyle Lifestyle { get; }
        internal string Name { get; }
        public ComponentRegistration(LifeStyle lifestyle, string name, IEnumerable<Type> serviceTypes, Type implementingType)
        {
            serviceTypes = serviceTypes.ToList();
            Contract.Arguments.That(serviceTypes.All(serviceType => serviceType.IsAssignableFrom(implementingType)), "The implementing type must implement all the service interfaces.");

            Name = name;
            ServiceTypes = serviceTypes;
            ImplementingType = implementingType;
            Lifestyle = lifestyle;
        }
    }
}
