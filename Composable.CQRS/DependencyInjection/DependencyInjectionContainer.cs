using System;
using Composable.DependencyInjection.Testing;
using JetBrains.Annotations;

namespace Composable.DependencyInjection
{
    public static class DependencyInjectionContainer
    {
        public static IServiceLocator SetupForTesting([InstantHandle]Action<IDependencyInjectionContainer> setup)
        {
            var @this = Create();


            @this.ConfigureWiringForTestsCallBeforeAllOtherWiring();

            setup(@this);

            @this.ConfigureWiringForTestsCallAfterAllOtherWiring();

            return @this.CreateServiceLocator();
        }

        internal static IDependencyInjectionContainer Create() => new Windsor.WindsorDependencyInjectionContainer();
    }
}