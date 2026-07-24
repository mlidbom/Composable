using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Sql.Sqlite;
using Compze.Sql.Sqlite._internal;
using Compze.Sql.Sqlite.Wiring;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tessaging._internal.SqlLayer;
using Compze.TypeIdentifiers.Interning;
using Compze.TypeIdentifiers.Interning.Sqlite.Wiring;
using Compze.Sql.Sqlite.Wiring._internal;
using Compze.Tessaging.Sqlite._private;

namespace Compze.Tessaging.Sqlite.Wiring;

public static class SqliteTessagingRegistrar
{
   extension(ExactlyOnceEndpointBuilder @this)
   {
      ///<summary>Declares the domain database this endpoint joins: sqlite, reached through <paramref name="connectionStringName"/> —<br/>
      /// filling the exactly-once endpoint's one domain-database parameter with the whole engine pairing: the connection pool,<br/>
      /// the sqlite type-id interner Tessaging's sql layers share (derived from the declaration), and Tessaging's sqlite sql layers.</summary>
      public ExactlyOnceEndpointBuilder SqliteDomainDatabase(string connectionStringName)
      {
         var domainDatabase = new SqliteDomainDatabase(connectionStringName);
         return @this.ConfigurePersistence(registrar => registrar.SqliteDomainDatabase(connectionStringName)
                                                          .SqliteTypeIdInterner(domainDatabase)
                                                          .SqliteTessagingSqlLayer(domainDatabase));
      }
   }

   ///<summary>Wires Tessaging's sqlite sql layers for the domain database an endpoint declared it joins<br/>
   /// (<paramref name="domainDatabase"/>). The peer registry's tables live in an auxiliary database of their own, named<br/>
   /// "«domain-database-name».PeerRegistry" — this is where that naming convention lives. Its own database gives the peer<br/>
   /// registry its own write gate, which is what lets an advertisement recording commit while a domain transaction is open —<br/>
   /// see <see cref="ISqlitePeerRegistryConnectionPool"/>.</summary>
   public static IComponentRegistrar SqliteTessagingSqlLayer(this IComponentRegistrar registrar, SqliteDomainDatabase domainDatabase) =>
      registrar.SqliteSchemaContribution((EndpointTableSet tables) => SqliteInboxSqlLayer.SchemaCreationSql(tables))
               .SqliteSchemaContribution((EndpointTableSet tables) => SqliteOutboxSqlLayer.SchemaCreationSql(tables))
               .SqliteSchemaContribution(SqliteEndpointCatalogSqlLayer.SchemaCreationSql)
               .SqliteAuxiliaryDatabaseConnectionPool<ISqlitePeerRegistryConnectionPool>($"{domainDatabase.ConnectionStringName}.PeerRegistry", getConnectionString => new ISqlitePeerRegistryConnectionPool.Pool(getConnectionString))
               .Register(
         Singleton.For<ITessagingSqlLayer.IEndpointCatalogSqlLayer>()
                  .CreatedBy((ISqliteConnectionPool endpointSqlConnection, SqliteSqlLayerSchemaManager schemaManager) => new SqliteEndpointCatalogSqlLayer(endpointSqlConnection, schemaManager)),
         Singleton.For<ITessagingSqlLayer.IOutboxSqlLayer>()
                  .CreatedBy((ISqliteConnectionPool endpointSqlConnection, SqliteSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner, EndpointTableSet tables) => new SqliteOutboxSqlLayer(endpointSqlConnection, schemaManager, typeIdInterner, tables)),
         Singleton.For<ITessagingSqlLayer.IInboxSqlLayer>()
                  .CreatedBy((ISqliteConnectionPool endpointSqlConnection, SqliteSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner, EndpointTableSet tables) => new SqliteInboxSqlLayer(endpointSqlConnection, schemaManager, typeIdInterner, tables)),
         Singleton.For<ITessagingSqlLayer.IPeerRegistrySqlLayer>()
                  .CreatedBy((ISqlitePeerRegistryConnectionPool peerRegistryConnectionPool, EndpointTableSet tables) =>
                                new SqlitePeerRegistrySqlLayer(peerRegistryConnectionPool,
                                                               new SqliteSqlLayerSchemaManager(peerRegistryConnectionPool, [SqlitePeerRegistrySqlLayer.SchemaCreationSql(tables)]),
                                                               tables)));
}
