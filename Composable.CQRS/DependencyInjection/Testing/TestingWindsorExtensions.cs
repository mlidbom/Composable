using Composable.System.Linq;
using Composable.SystemExtensions.Threading;

namespace Composable.DependencyInjection.Testing
{
    static class TestingWindsorExtensions
    {
        /// <summary>
        /// <para>SingleThreadUseGuard is registered for the component ISingleContextUseGuard</para>
        /// </summary>
        public static void ConfigureWiringForTestsCallBeforeAllOtherWiring(this IDependencyInjectionContainer container)
        {
            container.Register(
                               CComponent.For<TestModeMarker>()
                                         .ImplementedBy<TestModeMarker>()
                                         .LifestyleSingleton(),
                               CComponent.For<ISingleContextUseGuard>()
                                         .ImplementedBy<SingleThreadUseGuard>()
                                         .LifestyleScoped());
        }


        public static void ConfigureWiringForTestsCallAfterAllOtherWiring(this IDependencyInjectionContainer container)
        {
            container.CreateServiceLocator()
                     .UseAll<IConfigureWiringForTests>(components
                                                           => components.ForEach(component => component.ConfigureWiringForTesting()));
        }
    }
}
