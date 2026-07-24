using Compze.Abstractions.Configuration;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Sql.Sqlite._internal;
using Compze.Contracts;

namespace Compze.Sql.Sqlite.Wiring._internal;

///<summary>Registers the connection pool of an <em>auxiliary sqlite database</em>: a database of its consumer's own, beside the<br/>
/// domain database, with its own file and thus its own per-database write gate (see <see cref="ICompzeSqliteConnection"/>).<br/>
/// A consumer declares one by defining a pool type of its own — a marker interface extending <see cref="ISqliteConnectionPool"/>,<br/>
/// since the container resolves by service type and cannot hold two registrations of the same type — and registering it here<br/>
/// with the factory that constructs it.</summary>
///<remarks>Mirrors <see cref="SqliteConnectionPoolRegistrar"/>: in a test container it defers to the test database pool,<br/>
/// which hands every distinct <c>connectionStringName</c> its own pooled database; otherwise it reads the connection string<br/>
/// from configuration.</remarks>
static class SqliteAuxiliaryDatabaseConnectionPoolRegistrar
{
   ///<summary>Implemented by the testing infrastructure to point an auxiliary database's pool at a pooled test database instead of configuration.</summary>
   public interface ITestingRegistrar
   {
      public IComponentRegistrar Register<TPool>(string connectionStringName, Func<Func<string>, TPool> createPool) where TPool : class, ISqliteConnectionPool;
   }

   public static IComponentRegistrar SqliteAuxiliaryDatabaseConnectionPool<TPool>(this IComponentRegistrar registrar, string connectionStringName, Func<Func<string>, TPool> createPool) where TPool : class, ISqliteConnectionPool
   {
      //Pass the type argument explicitly at every call site: inferred from createPool's return type, TPool silently binds to
      //the concrete pool class, registering a service type no consumer resolves.
      Contract.Argument.Assert(typeof(TPool).IsInterface, () => $"{typeof(TPool).FullName} is a concrete type - register the auxiliary pool under the marker interface its consumers resolve: SqliteAuxiliaryDatabaseConnectionPool<IMyAuxiliaryPool>(...).");

      if(registrar.TryGetTestingRegistrar<ITestingRegistrar>() is {} testingRegistrar)
         return testingRegistrar.Register(connectionStringName, createPool);

      return registrar.Register(
         Singleton.For<TPool>()
                  .CreatedBy((IConfigurationParameterProvider configurationParameterProvider) => createPool(() => configurationParameterProvider.GetString(connectionStringName))));
   }
}
