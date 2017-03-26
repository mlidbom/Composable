using Castle.Windsor;
using Composable.DependencyInjection.Testing;

namespace Composable.DependencyInjection.Windsor.Testing
{
    static class TestingWindsorExtensions
    {
        public static void ConfigureWiringForTestsCallBeforeAllOtherWiring(this IWindsorContainer container)
        {
            container.AsDependencyInjectionContainer()
                     .ConfigureWiringForTestsCallBeforeAllOtherWiring();
        }

        public static void ConfigureWiringForTestsCallAfterAllOtherWiring(this IWindsorContainer container)
        {
            container.AsDependencyInjectionContainer()
                     .ConfigureWiringForTestsCallAfterAllOtherWiring();
        }
    }
}
