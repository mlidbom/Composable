using Compze.Sql.MicrosoftSql._internal;
using Compze.Sql.MySql._internal;
using Compze.Sql.PostgreSql._internal;
using Compze.Sql.Sqlite._internal;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Sql.MicrosoftSql.Wiring._internal;
using Compze.Sql.MySql.Wiring._internal;
using Compze.Sql.PostgreSql.Wiring._internal;
using Compze.Sql.Sqlite.Wiring._internal;

namespace Compze.Hosting.Testing.Wiring;

///<summary>
/// The component registrar all test containers are built with. It is what lets production wiring transparently use
/// pooled test databases: when a production registrar such as <c>MsSqlConnectionPool(connectionStringName)</c> asks
/// <see cref="TryGetTestingRegistrar{TTestingRegistrar}"/> for a testing override, this registrar supplies one that
/// resolves connection strings through the test database pool instead of application configuration.
///</summary>
public class TestingComponentRegistrar : ComponentRegistrar
{
   readonly IDictionary<Type, object> _testingRegistrars;

   public TestingComponentRegistrar()
   {
      _testingRegistrars = new Dictionary<Type, object>
                           {
                              { typeof(MsSqlSqlConnectionPoolRegistrar.ITestingRegistrar), new MsSqlDbPoolRegistrar(this) },
                              { typeof(MySqlConnectionPoolRegistrar.ITestingRegistrar), new MySqlDbPoolRegistrar(this) },
                              { typeof(PgSqlConnectionPoolRegistrar.ITestingRegistrar), new PostgreSqlSqlDbPoolRegistrar(this) },
                              { typeof(SqliteConnectionPoolRegistrar.ITestingRegistrar), new SqliteSqlDbPoolRegistrar(this) },
                              { typeof(SqliteMemoryConnectionPoolRegistrar.ITestingRegistrar), new SqliteMemoryDbPoolRegistrar(this) },
                              { typeof(SqliteTypeIdInternerConnectionPoolRegistrar.ITestingRegistrar), new SqliteTypeIdInternerDbPoolRegistrar(this) },
                              { typeof(SqliteAuxiliaryDatabaseConnectionPoolRegistrar.ITestingRegistrar), new SqliteAuxiliaryDatabaseDbPoolRegistrar(this) }
                           };
   }

   public override TTestingRegistrar? TryGetTestingRegistrar<TTestingRegistrar>() where TTestingRegistrar : class
   {
      if(_testingRegistrars.TryGetValue(typeof(TTestingRegistrar), out var value))
      {
         return (TTestingRegistrar)value;
      }

      return null;
   }

   public override IComponentRegistrar Clone() => new TestingComponentRegistrar();

   class MsSqlDbPoolRegistrar(IComponentRegistrar registrar) : MsSqlSqlConnectionPoolRegistrar.ITestingRegistrar
   {
      readonly IComponentRegistrar _registrar = registrar;

      public IComponentRegistrar Register(string connectionStringName) => _registrar.CurrentTestsDbPoolIfNotCloneContainer()
                                                                                    .Register(
                                                                                        Singleton.For<IMsSqlConnectionPool>()
                                                                                                 .CreatedBy((global::Compze.DbPool.DbPool dbPool) =>
                                                                                                               IMsSqlConnectionPool.CreateInstance(() => dbPool.ConnectionStringFor(connectionStringName))));
   }

   class MySqlDbPoolRegistrar(IComponentRegistrar registrar) : MySqlConnectionPoolRegistrar.ITestingRegistrar
   {
      readonly IComponentRegistrar _registrar = registrar;

      public IComponentRegistrar Register(string connectionStringName) => _registrar.CurrentTestsDbPoolIfNotCloneContainer()
                                                                                    .Register(
                                                                                        Singleton.For<IMySqlConnectionPool>()
                                                                                                 .CreatedBy((global::Compze.DbPool.DbPool dbPool) =>
                                                                                                               IMySqlConnectionPool.CreateInstance(() => dbPool.ConnectionStringFor(connectionStringName))));
   }

   class PostgreSqlSqlDbPoolRegistrar(IComponentRegistrar registrar) : PgSqlConnectionPoolRegistrar.ITestingRegistrar
   {
      readonly IComponentRegistrar _registrar = registrar;

      public IComponentRegistrar Register(string connectionStringName) => _registrar.CurrentTestsDbPoolIfNotCloneContainer()
                                                                                    .Register(
                                                                                        Singleton.For<IPgSqlConnectionPool>()
                                                                                                 .CreatedBy((global::Compze.DbPool.DbPool pool) => IPgSqlConnectionPool.CreateInstance1(() => pool.ConnectionStringFor(connectionStringName))));
   }

   class SqliteSqlDbPoolRegistrar(IComponentRegistrar registrar) : SqliteConnectionPoolRegistrar.ITestingRegistrar
   {
      readonly IComponentRegistrar _registrar = registrar;

      public IComponentRegistrar Register(string connectionStringName) => _registrar.CurrentTestsDbPoolIfNotCloneContainer()
                                                                                    .Register(
                                                                                        Singleton.For<ISqliteConnectionPool>()
                                                                                                 .CreatedBy((global::Compze.DbPool.DbPool pool) => ISqliteConnectionPool.CreateInstance(() => pool.ConnectionStringFor(connectionStringName))));
   }

   class SqliteMemoryDbPoolRegistrar(IComponentRegistrar registrar) : SqliteMemoryConnectionPoolRegistrar.ITestingRegistrar
   {
      readonly IComponentRegistrar _registrar = registrar;

      public IComponentRegistrar Register(string connectionStringName) => _registrar.CurrentTestsDbPoolIfNotCloneContainer()
                                                                                    .Register(
                                                                                        Singleton.For<ISqliteConnectionPool>()
                                                                                                 .CreatedBy((global::Compze.DbPool.DbPool pool) => ISqliteConnectionPool.CreateInstance(() => pool.ConnectionStringFor(connectionStringName))));
   }

   class SqliteTypeIdInternerDbPoolRegistrar(IComponentRegistrar registrar) : SqliteTypeIdInternerConnectionPoolRegistrar.ITestingRegistrar
   {
      readonly IComponentRegistrar _registrar = registrar;

      public IComponentRegistrar Register(string connectionStringName) => _registrar.CurrentTestsDbPoolIfNotCloneContainer()
                                                                                    .Register(
                                                                                        Singleton.For<ISqliteTypeIdInternerConnectionPool>()
                                                                                                 .CreatedBy((global::Compze.DbPool.DbPool pool) => new ISqliteTypeIdInternerConnectionPool.Pool(() => pool.ConnectionStringFor(connectionStringName))));
   }

   class SqliteAuxiliaryDatabaseDbPoolRegistrar(IComponentRegistrar registrar) : SqliteAuxiliaryDatabaseConnectionPoolRegistrar.ITestingRegistrar
   {
      readonly IComponentRegistrar _registrar = registrar;

      public IComponentRegistrar Register<TPool>(string connectionStringName, Func<Func<string>, TPool> createPool) where TPool : class, ISqliteConnectionPool =>
         _registrar.CurrentTestsDbPoolIfNotCloneContainer()
                   .Register(
                       Singleton.For<TPool>()
                                .CreatedBy((global::Compze.DbPool.DbPool pool) => createPool(() => pool.ConnectionStringFor(connectionStringName))));
   }
}
