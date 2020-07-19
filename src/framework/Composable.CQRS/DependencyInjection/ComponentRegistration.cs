using System;
using System.Collections.Generic;
using System.Linq;
using Composable.Contracts;
using Composable.Logging;
using Composable.Persistence.EventStore;
using Composable.Serialization;
using Composable.System;
using Composable.System.Diagnostics;
using Composable.System.Linq;
using Composable.System.Reflection;

// ReSharper disable UnusedMember.Global

namespace Composable.DependencyInjection
{
    public static class Singleton
    {
        public static SingletonRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3, TService4, TService5, TService6, TService7, TService8, TService9>() where TService1 : class => For<TService1>(Seq.OfTypes<TService2, TService3, TService4, TService5, TService6, TService7, TService8, TService9>());
        public static SingletonRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3, TService4, TService5, TService6, TService7, TService8>() where TService1 : class => For<TService1>(Seq.OfTypes<TService2, TService3, TService4, TService5, TService6, TService7, TService8>());
        public static SingletonRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3, TService4, TService5, TService6, TService7>() where TService1 : class => For<TService1>(Seq.OfTypes<TService2, TService3, TService4, TService5, TService6, TService7>());
        public static SingletonRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3, TService4, TService5, TService6>() where TService1 : class => For<TService1>(Seq.OfTypes<TService2, TService3, TService4, TService5, TService6>());
        public static SingletonRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3, TService4, TService5>() where TService1 : class => For<TService1>(Seq.OfTypes<TService2, TService3, TService4, TService5>());
        public static SingletonRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3, TService4>() where TService1 : class => For<TService1>(Seq.OfTypes<TService2, TService3, TService4>());
        public static SingletonRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3>() where TService1 : class => For<TService1>(Seq.OfTypes<TService2, TService3>());
        public static SingletonRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2>() where TService1 : class => For<TService1>(Seq.OfTypes<TService2>());
        public static SingletonRegistrationWithoutInstantiationSpec<TService> For<TService>() where TService : class => For<TService>(new List<Type>());
        static SingletonRegistrationWithoutInstantiationSpec<TService> For<TService>(IEnumerable<Type> additionalServices) where TService : class => new SingletonRegistrationWithoutInstantiationSpec<TService>(additionalServices);
    }

    public static class Scoped
    {
        public static ComponentRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3, TService4, TService5, TService6, TService7, TService8, TService9>() where TService1 : class => For<TService1>(Seq.OfTypes<TService2, TService3, TService4, TService5, TService6, TService7, TService8, TService9>());
        public static ComponentRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3, TService4, TService5, TService6, TService7, TService8>() where TService1 : class => For<TService1>(Seq.OfTypes<TService2, TService3, TService4, TService5, TService6, TService7, TService8>());
        public static ComponentRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3, TService4, TService5, TService6, TService7>() where TService1 : class => For<TService1>(Seq.OfTypes<TService2, TService3, TService4, TService5, TService6, TService7>());
        public static ComponentRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3, TService4, TService5, TService6>() where TService1 : class => For<TService1>(Seq.OfTypes<TService2, TService3, TService4, TService5, TService6>());
        public static ComponentRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3, TService4, TService5>() where TService1 : class => For<TService1>(Seq.OfTypes<TService2, TService3, TService4, TService5>());
        public static ComponentRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3, TService4>() where TService1 : class => For<TService1>(Seq.OfTypes<TService2, TService3, TService4>());
        public static ComponentRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3>() where TService1 : class => For<TService1>(Seq.OfTypes<TService2, TService3>());
        public static ComponentRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2>() where TService1 : class => For<TService1>(Seq.OfTypes<TService2>());
        public static ComponentRegistrationWithoutInstantiationSpec<TService> For<TService>() where TService : class => For<TService>(new List<Type>());
        static ComponentRegistrationWithoutInstantiationSpec<TService> For<TService>(IEnumerable<Type> additionalServices) where TService : class => new ComponentRegistrationWithoutInstantiationSpec<TService>(Lifestyle.Scoped, additionalServices);
    }

    public class ComponentRegistrationWithoutInstantiationSpec<TService> where TService : class
    {
        protected IReadOnlyList<Type> ServiceTypes { get; }
        readonly Lifestyle _lifestyle;
        internal ComponentRegistrationWithoutInstantiationSpec(Lifestyle lifestyle, IEnumerable<Type> serviceTypes)
        {
            _lifestyle = lifestyle;
            ServiceTypes = serviceTypes.Concat(new List<Type> {typeof(TService)}).ToList();
        }

        internal ComponentRegistration<TService> CreatedBy<TImplementation>(Func<IServiceLocatorKernel, TImplementation> factoryMethod)
            where TImplementation : TService
        {
            var implementationType = typeof(TImplementation);
            AssertImplementsAllServices(implementationType);
            return new ComponentRegistration<TService>(_lifestyle, ServiceTypes, InstantiationSpec.FromFactoryMethod(serviceLocator => factoryMethod(serviceLocator), implementationType));
        }

        protected void AssertImplementsAllServices(Type implementationType)
        {
            var unImplementedService = ServiceTypes.FirstOrDefault(serviceType => !serviceType.IsAssignableFrom(implementationType));
            if(unImplementedService != null)
            {
                throw new ArgumentException($"{implementationType.FullName} does not implement: {unImplementedService.FullName}");
            }
        }
    }

    public class SingletonRegistrationWithoutInstantiationSpec<TService> : ComponentRegistrationWithoutInstantiationSpec<TService> where TService : class
    {
        internal SingletonRegistrationWithoutInstantiationSpec(IEnumerable<Type> serviceTypes) : base(Lifestyle.Singleton, serviceTypes) {}

        internal ComponentRegistration<TService> Instance(TService instance)
        {
            AssertImplementsAllServices(instance.GetType());
            return new ComponentRegistration<TService>(Lifestyle.Singleton, ServiceTypes, InstantiationSpec.FromInstance(instance));
        }
    }

    class InstantiationSpec
    {
        static readonly bool LogSlowConstructions = false;
        internal object? SingletonInstance { get; }
        internal object RunFactoryMethod(IServiceLocatorKernel kern) => FactoryMethod!(kern);
        internal Func<IServiceLocatorKernel, object> FactoryMethod { get; }
        internal Type FactoryMethodReturnType { get; }

        internal static InstantiationSpec FromInstance(object instance) => new InstantiationSpec(instance);

        internal static InstantiationSpec FromFactoryMethod(Func<IServiceLocatorKernel, object> factoryMethod, Type factoryMethodReturnType) => new InstantiationSpec(factoryMethod, factoryMethodReturnType);

        InstantiationSpec(Func<IServiceLocatorKernel, object> factoryMethod, Type factoryMethodReturnType)
        {
            FactoryMethodReturnType = factoryMethodReturnType;

            if(!LogSlowConstructions)
            {
                FactoryMethod = factoryMethod;
            } else
            {
                FactoryMethod = kern =>
                {

                    object instance = null!;

                    IEnumerable<string> ignoredTypePatterns = Seq.Create(nameof(EventCache), //Constructing the system provided cache is slow.
                                                                         nameof(EventStoreSerializer),//Caused by NewtonsoftJson initialization that is static and happens only once.
                                                                         "DatabasePool"//This is slow the first time because it sets up the MachineWideSharedObject
                        );

                    var constructionTime = StopwatchExtensions.TimeExecution(() => instance = factoryMethod(kern));
                    if(constructionTime > 5.Milliseconds() && ignoredTypePatterns.None(ignored => factoryMethodReturnType.Name.ContainsInvariant(ignored)))
                    {
                        this.Log().Warning($"###########################################################: Component: {factoryMethodReturnType.GetFullNameCompilable()} took: {constructionTime:ss\\.fff} to construct");
                    }

                    return instance;
                };
            }
        }

        InstantiationSpec(object singletonInstance)
        {
            Assert.Argument.NotNull(singletonInstance);
            SingletonInstance = singletonInstance;
            FactoryMethod = kern => singletonInstance;
            FactoryMethodReturnType = singletonInstance.GetType();
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

        internal ComponentRegistration(Lifestyle lifestyle, IEnumerable<Type> serviceTypes, InstantiationSpec instantiationSpec)
        {
            serviceTypes = serviceTypes.ToList();

            ServiceTypeIndexes = serviceTypes.Select(ComposableDependencyInjectionContainer.ServiceTypeIndex.For).ToArray();
            Contract.Arguments.That(lifestyle == Lifestyle.Singleton || instantiationSpec.SingletonInstance == null, $"{nameof(InstantiationSpec.SingletonInstance)} registrations must be {nameof(Lifestyle.Singleton)}s");

            ServiceTypes = serviceTypes;
            InstantiationSpec = instantiationSpec;
            Lifestyle = lifestyle;
        }

        internal abstract ComponentRegistration CreateCloneRegistration(IServiceLocator currentLocator);

        internal abstract object Resolve(IServiceLocator serviceLocator);
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

            Assert.State.Assert(Lifestyle == Lifestyle.Singleton);
            //We must use singleton instance registrations when delegating because otherwise the containers will both attempt to dispose the service.
            //Instance registrations are not disposed.
            return new ComponentRegistration<TService>(
                lifestyle: Lifestyle.Singleton,
                serviceTypes: ServiceTypes,
                instantiationSpec: InstantiationSpec.FromInstance(currentLocator.Resolve<TService>())
            );
        }

        internal override object Resolve(IServiceLocator locator) => locator.Resolve<TService>();

        internal ComponentRegistration(Lifestyle lifestyle, IEnumerable<Type> serviceTypes, InstantiationSpec instantiationSpec)
            :base(lifestyle, serviceTypes, instantiationSpec)
        {}
    }
}
