using Compze.Sql.MicrosoftSql.Wiring;
using Compze.Sql.MySql.Wiring;
using Compze.Sql.PostgreSql.Wiring;
using Compze.Sql.Sqlite;
using Compze.Sql.Sqlite.Wiring;
using Compze.DocumentDb.MicrosoftSql.Wiring;
using Compze.DocumentDb.MySql.Wiring;
using Compze.DocumentDb.PostgreSql.Wiring;
using Compze.DocumentDb.Sqlite.Wiring;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Tessaging.MicrosoftSql.Wiring;
using Compze.Tessaging.MySql.Wiring;
using Compze.Tessaging.PostgreSql.Wiring;
using Compze.Tessaging.Sqlite.Wiring;
using Compze.TypeIdentifiers.Interning.Sqlite.Wiring;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging._internal.SqlLayer;
using Compze.Internals.SystemCE;
using Compze.Internals.Testing;
using Compze.Teventive.TeventStore.MicrosoftSql.Wiring;
using Compze.Teventive.TeventStore.MySql.Wiring;
using Compze.Teventive.TeventStore.PostgreSql.Wiring;
using Compze.Teventive.TeventStore.Sqlite.Wiring;

namespace Compze.Tessaging.Hosting.Testing.Wiring;

public static class TestingComponentRegistrarSqlLayer
{
   ///<summary>
   /// Registers, for the SQL backend the current test runs against, the persistence the Tessaging vertical's
   /// storage stack uses: the type-id interner, the document db, the tessaging inbox/outbox, and the tevent store —
   /// including one schema manager that creates all of their schemas.
   ///</summary>
   public static IComponentRegistrar CurrentTestsConfiguredSqlLayer(this IComponentRegistrar register, string connectionStringName)
   {
      //A plain testing container is not an endpoint, so no composition declares the table-set Tessaging's sql layers prefix
      //their tables with - declare one for the container. An endpoint composition registers the endpoint's own set first.
      if(!register.IsRegistered<EndpointTableSet>())
         register.Register(Singleton.For<EndpointTableSet>().Instance(EndpointTableSet.For(new EndpointConfiguration("TestingContainer", new EndpointId(Guid.NewGuid())))));

      return register.CastTo<TestingComponentRegistrar>()
              .CurrentTestsConfiguredSqlLayer(connectionStringName);
   }

   static IComponentRegistrar CurrentTestsConfiguredSqlLayer(this TestingComponentRegistrar @this, string connectionStringName) =>
      TestEnv.SqlLayer switch
      {
         SqlLayer.MsSql => @this.MsSqlDomainDatabase(connectionStringName)
                                .MsSqlDocumentDbSqlLayer()
                                .MsSqlTessagingSqlLayer()
                                .MsSqlTeventStoreSqlLayer(),
         SqlLayer.MySql => @this.MySqlDomainDatabase(connectionStringName)
                                .MySqlDocumentDbSqlLayer()
                                .MySqlTessagingSqlLayer()
                                .MySqlTeventStoreSqlLayer(),
         SqlLayer.PgSql => @this.PgSqlDomainDatabase(connectionStringName)
                                .PgSqlDocumentDbSqlLayer()
                                .PgSqlTessagingSqlLayer()
                                .PgSqlTeventStoreSqlLayer(),
         SqlLayer.Sqlite => @this.SqliteDomainDatabase(connectionStringName)
                                 .SqliteTypeIdInterner($"{connectionStringName}.TypeIdInterner")
                                 .SqliteDocumentDbSqlLayer()
                                 .SqliteTessagingSqlLayer(new SqliteDomainDatabase(connectionStringName))
                                 .SqliteTeventStoreSqlLayer(),
         SqlLayer.SqliteMemory => @this.SqliteMemoryDomainDatabase(connectionStringName)
                                       .SqliteTypeIdInterner($"{connectionStringName}.TypeIdInterner")
                                       .SqliteDocumentDbSqlLayer()
                                       .SqliteTessagingSqlLayer(new SqliteDomainDatabase(connectionStringName))
                                       .SqliteTeventStoreSqlLayer(),
         _ => throw new ArgumentOutOfRangeException()
      };
}
