using System;
using Composable.DependencyInjection.SimpleInjectorImplementation;
using Composable.DependencyInjection.Testing;
using JetBrains.Annotations;

namespace Composable.DependencyInjection
{
    public static class DependencyInjectionContainer
    {
        public static IServiceLocator CreateServiceLocatorForTesting() => CreateServiceLocatorForTesting(_ => {}, TestingMode.DatabasePool);

        public static IServiceLocator CreateServiceLocatorForTesting(TestingMode mode) => CreateServiceLocatorForTesting(_ => {}, mode);

        public static IServiceLocator CreateServiceLocatorForTesting([InstantHandle] Action<IDependencyInjectionContainer> setup) => CreateServiceLocatorForTesting(setup, TestingMode.DatabasePool);

        public static IServiceLocator CreateServiceLocatorForTesting([InstantHandle]Action<IDependencyInjectionContainer> setup, TestingMode mode)
        {
            var @this = Create(new RunMode(isTesting:true, testingMode: mode));


            @this.ConfigureWiringForTestsCallBeforeAllOtherWiring();

            setup(@this);

            return @this.CreateServiceLocator();
        }

        public static IDependencyInjectionContainer Create(IRunMode runMode = null)
        {
            IDependencyInjectionContainer container = new SimpleInjectorDependencyInjectionContainer(runMode ?? DependencyInjection.RunMode.Production);
            container.Register(Component.For<IServiceLocator>()
                                        .UsingFactoryMethod(() => container.CreateServiceLocator())
                                        .LifestyleSingleton());
            return container;
        }
    }
}