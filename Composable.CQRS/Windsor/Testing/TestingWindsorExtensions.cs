using Castle.Windsor;
using Composable.DependencyInjection;
using Composable.DependencyInjection.Testing;
using Composable.System.Linq;
using Composable.SystemExtensions.Threading;

namespace Composable.Windsor.Testing
{
    static class TestingWindsorExtensions
    {
        /// <summary>
        /// <para>SingleThreadUseGuard is registered for the component ISingleContextUseGuard</para>
        /// </summary>
        public static void ConfigureWiringForTestsCallBeforeAllOtherWiring(this IWindsorContainer container)
        {
            container.AsDependencyInjectionContainer()
                     .Register(
                               CComponent.For<TestModeMarker>()
                                         .ImplementedBy<TestModeMarker>()
                                         .LifestyleSingleton(),
                               CComponent.For<ISingleContextUseGuard>()
                                         .ImplementedBy<SingleThreadUseGuard>()
                                         .LifestyleScoped());
        }


        public static void ConfigureWiringForTestsCallAfterAllOtherWiring(this IWindsorContainer container)
        {
            container.AsServiceLocator()
                     .UseAll<IConfigureWiringForTests>(components
                                                           => components.ForEach(component => component.ConfigureWiringForTesting()));
        }
    }
}
