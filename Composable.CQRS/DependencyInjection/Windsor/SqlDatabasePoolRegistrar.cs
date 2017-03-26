using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.Testing;

namespace Composable.DependencyInjection.Windsor
{
    static class SqlDatabasePoolRegistrar
    {
        internal static SqlServerDatabasePool RegisterSqlServerDatabasePool(this IWindsorContainer @this,  string connectionString)
        {
            @this.Register(
                Component.For<SqlServerDatabasePool>()
                .UsingFactoryMethod(() => new SqlServerDatabasePool(connectionString))
                .LifestyleSingleton()
                .Named(connectionString)
                );

            return @this.Resolve<SqlServerDatabasePool>(connectionString);
        }
    }
}