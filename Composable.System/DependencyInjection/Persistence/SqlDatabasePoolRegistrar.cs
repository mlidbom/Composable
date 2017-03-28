using Composable.Testing;

namespace Composable.DependencyInjection.Persistence
{
    static class SqlDatabasePoolRegistrar
    {
        internal static SqlServerDatabasePool RegisterSqlServerDatabasePool(this IDependencyInjectionContainer @this,  string connectionString)
        {
            @this.Register(
                CComponent.For<SqlServerDatabasePool>()
                .UsingFactoryMethod(locator => new SqlServerDatabasePool(connectionString))
                .Named(connectionString)
                .LifestyleSingleton()
                );

            return @this.CreateServiceLocator().Resolve<SqlServerDatabasePool>(connectionString);
        }
    }
}