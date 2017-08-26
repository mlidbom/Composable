using System;
using Composable.DependencyInjection.SimpleInjectorImplementation;
using Composable.DependencyInjection.Testing;
using JetBrains.Annotations;

namespace Composable.DependencyInjection
{
    public static class DependencyInjectionContainer
    {
        public static IServiceLocator CreateServiceLocatorForTesting(TestingMode mode = TestingMode.DatabasePool) => CreateServiceLocatorForTesting(_ => {}, mode);

        public static IServiceLocator CreateServiceLocatorForTesting([InstantHandle]Action<IDependencyInjectionContainer> setup, TestingMode mode = TestingMode.DatabasePool)
        {
            var @this = Create(new RunMode(isTesting:true, mode: mode));


            @this.ConfigureWiringForTestsCallBeforeAllOtherWiring(mode);

            setup(@this);

            return @this.CreateServiceLocator();
        }

        internal static IDependencyInjectionContainer Create(IRunMode runMode = null) => new SimpleInjectorDependencyInjectionContainer(runMode ?? DependencyInjection.RunMode.Production);
    }
}