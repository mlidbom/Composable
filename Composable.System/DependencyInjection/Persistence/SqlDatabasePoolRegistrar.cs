using Composable.Testing;

namespace Composable.DependencyInjection.Persistence
{
    static class SqlDatabasePoolRegistrar
    {
        internal static SqlServerDatabasePool RegisterSqlServerDatabasePool(this IDependencyInjectionContainer @this,  string connectionString)
        {
            var sqlServerDatabasePool = new SqlServerDatabasePool(connectionString);
            @this.Register(
                Component.For<SqlServerDatabasePool>()
                .UsingFactoryMethod(_ => sqlServerDatabasePool)
                .LifestyleSingleton()
                );

            return @this.CreateServiceLocator().Resolve<SqlServerDatabasePool>();
        }
    }
}