using Castle.MicroKernel.Lifestyle;
using Castle.Windsor;

namespace Composable.CQRS.Windsor.Testing
{
    public static class TestingWindsorExtensions
    {
        public static void ResetTestDataBases(this IWindsorContainer container)
        {
            using (container.BeginScope())
            {
                foreach (var dbResetter in container.ResolveAll<IResetTestDatabases>())
                {
                    dbResetter.ResetDatabase();
                    container.Release(dbResetter);
                }
            }
        }

        public static void ConfigureWiringForTests(this IWindsorContainer container)
        {
            foreach (var configurer in container.ResolveAll<IConfigureWiringForTests>())
            {
                configurer.ConfigureWiringForTesting();
                container.Release(configurer);
            }
        }
    }
}