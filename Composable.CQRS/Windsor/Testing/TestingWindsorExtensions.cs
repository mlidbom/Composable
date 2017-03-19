using Castle.Core;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.SystemExtensions.Threading;

namespace Composable.Windsor.Testing
{
    static class TestingWindsorExtensions
    {
        /// <summary>
        ///<para>Components registered as PerWebRequest will be remapped to Scoped.</para>
        /// <para>SingleThreadUseGuard is registered for the component ISingleContextUseGuard</para>
        /// </summary>
        public static void ConfigureWiringForTestsCallBeforeAllOtherWiring(this IWindsorContainer container)
        {
            container.Kernel.ComponentModelBuilder.AddContributor(
                new LifestyleRegistrationMutator(originalLifestyle: LifestyleType.PerWebRequest, newLifestyleType: LifestyleType.Scoped)
                );

            container.Register(
                Component.For<ISingleContextUseGuard>()
                    .ImplementedBy<SingleThreadUseGuard>()
                    .LifestyleScoped()
                );
        }


        public static void ConfigureWiringForTestsCallAfterAllOtherWiring(this IWindsorContainer container)
        {
            foreach(var configurer in container.ResolveAll<IConfigureWiringForTests>())
            {
                configurer.ConfigureWiringForTesting();
                container.Release(configurer);
            }
        }
    }
}
