using Castle.Core;
using Castle.Windsor;
using Composable.DependencyInjection;
using Composable.DependencyInjection.Testing;
using Composable.System.Linq;
using Composable.SystemExtensions.Threading;
using Component = Castle.MicroKernel.Registration.Component;

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
                     .Register(CComponent.For<ISingleContextUseGuard>()
                                         .ImplementedBy<SingleThreadUseGuard>()
                                         .LifestyleScoped());
        }


        public static void ConfigureWiringForTestsCallAfterAllOtherWiring(this IWindsorContainer container)
        {
            //container.AsServiceLocator()
            //         .UseAll<IConfigureWiringForTests>(components
            //                                               => components.ForEach(component => component.ConfigureWiringForTesting()));

            foreach (var configurer in container.ResolveAll<IConfigureWiringForTests>())
            {
                configurer.ConfigureWiringForTesting();
                container.Release(configurer);
            }
        }
    }
}
