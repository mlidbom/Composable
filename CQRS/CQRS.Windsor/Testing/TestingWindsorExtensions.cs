using Castle.Core;
using Castle.MicroKernel.Lifestyle;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.SystemExtensions.Threading;

namespace Composable.CQRS.Windsor.Testing
{
    public static class TestingWindsorExtensions
    {
        public static void ResetTestDataBases(this IWindsorContainer container)
        {
            using(container.BeginScope())
            {
                foreach(var dbResetter in container.ResolveAll<IResetTestDatabases>())
                {
                    dbResetter.ResetDatabase();
                    container.Release(dbResetter);
                }
            }
        }

        public static void ConfigureWiringForTestsCallBeforeAllOtherWiring(this IWindsorContainer container)
        {
            container.Kernel.ComponentModelBuilder.AddContributor(
                new LifestyleRegistrationMutator(originalLifestyle: LifestyleType.PerWebRequest, newLifestyleType: LifestyleType.Scoped)
                );

            container.Register(Component.For<ISingleContextUseGuard>().ImplementedBy<SingleThreadUseGuard>());
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
