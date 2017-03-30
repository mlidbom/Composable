using System;
using Composable.DependencyInjection.Testing;
using JetBrains.Annotations;

namespace Composable.DependencyInjection
{
    public static class DependencyInjectionContainer
    {
        internal static IRunMode RunMode(this IDependencyInjectionContainer @this) => @this.CreateServiceLocator()
                                                                                  .Resolve<IRunMode>();

        public static IServiceLocator CreateServiceLocatorForTesting([InstantHandle]Action<IDependencyInjectionContainer> setup)
        {
            var @this = Create();


            @this.ConfigureWiringForTestsCallBeforeAllOtherWiring();

            setup(@this);

            return @this.CreateServiceLocator();
        }

        internal static IDependencyInjectionContainer Create() => new Windsor.WindsorDependencyInjectionContainer();
    }
}