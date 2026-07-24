using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Endpoints;
using Compze.Hosting;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.Testing;
using Compze.Must;
using Compze.Sql.MicrosoftSql.Wiring;
using Compze.Sql.MySql.Wiring;
using Compze.Sql.PostgreSql.Wiring;
using Compze.Sql.Sqlite;
using Compze.Sql.Sqlite.Wiring;
using Compze.DocumentDb.MicrosoftSql.Wiring;
using Compze.DocumentDb.MySql.Wiring;
using Compze.DocumentDb.PostgreSql.Wiring;
using Compze.DocumentDb.Sqlite.Wiring;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tessaging.MicrosoftSql.Wiring;
using Compze.Tessaging.MySql.Wiring;
using Compze.Tessaging.PostgreSql.Wiring;
using Compze.Tessaging.Sqlite.Wiring;
using Compze.Tessaging.TessageTypes;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Teventive.TeventStore.MicrosoftSql.Wiring;
using Compze.Teventive.TeventStore.MySql.Wiring;
using Compze.Teventive.TeventStore.PostgreSql.Wiring;
using Compze.Teventive.TeventStore.Sqlite.Wiring;
using Compze.TypeIdentifiers.Interning.Sqlite.Wiring;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.Hosting;

///<summary>
/// The consumer-proof for the domain-database registrar extensions: an exactly-once endpoint whose environment declares its persistence
/// exactly the way a consumer declares it — the current backend's public registrar extension (<c>MsSqlDomainDatabase(...)</c>,
/// <c>MySqlDomainDatabase(...)</c>, <c>PgSqlDomainDatabase(...)</c>, <c>SqliteDomainDatabase(...)</c>,
/// <c>SqliteMemoryDomainDatabase(...)</c>) followed by the feature sql layers, never the testing hosts' backend switch. The
/// endpoint then converses with itself, proving the whole durable vertical — schema creation included — stands on the
/// extension-declared pool.
///</summary>
public class Given_an_exactly_once_endpoint_declaring_its_domain_database_through_the_public_backend_registrar_extensions : UniversalTestBase
{
   readonly IEndpointHost _host;
   readonly ExactlyOnceEndpoint _endpoint;
   bool _tommandHandled;

   public Given_an_exactly_once_endpoint_declaring_its_domain_database_through_the_public_backend_registrar_extensions()
   {
      _host = EndpointHost.Production.Create(() => TestEnv.DIContainer.CreateTestingContainerBuilder()
                                                          ._mutate(it => it.Registrar.CurrentTestsDbPoolIfNotCloneContainer()),
                                             new EnvironmentDeclaringTheDomainDatabaseThroughThePublicBackendRegistrarExtensions());

      _endpoint = _host.RegisterEndpoint(new PublicApiDomainDatabaseEndpointDeclaration(this));
   }

   ///<summary>The environment whose database binding is this specification's point: the domain database declared exactly the<br/>
   /// way a consumer declares it — the current backend's public registrar extension followed by the feature sql layers, never the<br/>
   /// testing hosts' backend switch — plus the current test's transport.</summary>
   class EnvironmentDeclaringTheDomainDatabaseThroughThePublicBackendRegistrarExtensions : IEndpointEnvironment
   {
      public void Configure(EndpointBuilder endpointBuilder) =>
         endpointBuilder.TransportProtocol(registrar => registrar.CurrentTestsEndpointTransport());

      public void ConfigureDomainDatabase(ExactlyOnceEndpointBuilder endpointBuilder) =>
         endpointBuilder.ConfigurePersistence(registrar => DeclareDomainDatabaseThroughThePublicRegistrarExtensions(registrar, connectionStringName: endpointBuilder.Configuration.Id.ToString()));

      static IComponentRegistrar DeclareDomainDatabaseThroughThePublicRegistrarExtensions(IComponentRegistrar registrar, string connectionStringName) =>
         TestEnv.SqlLayer switch
         {
            SqlLayer.MsSql => registrar.MsSqlDomainDatabase(connectionStringName)
                                       .MsSqlDocumentDbSqlLayer()
                                       .MsSqlTessagingSqlLayer()
                                       .MsSqlTeventStoreSqlLayer(),
            SqlLayer.MySql => registrar.MySqlDomainDatabase(connectionStringName)
                                       .MySqlDocumentDbSqlLayer()
                                       .MySqlTessagingSqlLayer()
                                       .MySqlTeventStoreSqlLayer(),
            SqlLayer.PgSql => registrar.PgSqlDomainDatabase(connectionStringName)
                                       .PgSqlDocumentDbSqlLayer()
                                       .PgSqlTessagingSqlLayer()
                                       .PgSqlTeventStoreSqlLayer(),
            //The declaration-object interner form rides the on-disk branch so the one extension that takes a SqliteDomainDatabase is consumer-proven too.
            SqlLayer.Sqlite => registrar.SqliteDomainDatabase(connectionStringName)
                                        .SqliteTypeIdInterner(new SqliteDomainDatabase($"{connectionStringName}.TypeIdInterner"))
                                        .SqliteDocumentDbSqlLayer()
                                        .SqliteTessagingSqlLayer(new SqliteDomainDatabase(connectionStringName))
                                        .SqliteTeventStoreSqlLayer(),
            SqlLayer.SqliteMemory => registrar.SqliteMemoryDomainDatabase(connectionStringName)
                                              .SqliteTypeIdInterner($"{connectionStringName}.TypeIdInterner")
                                              .SqliteDocumentDbSqlLayer()
                                              .SqliteTessagingSqlLayer(new SqliteDomainDatabase(connectionStringName))
                                              .SqliteTeventStoreSqlLayer(),
            _ => throw new ArgumentOutOfRangeException()
         };
   }

   class PublicApiDomainDatabaseEndpointDeclaration : ExactlyOnceEndpointDeclaration<PublicApiDomainDatabaseEndpointDeclaration>, IEndpointIdentity
   {
      public static string Name => "PublicApiDomainDatabase";
      public static EndpointId Id { get; } = new(Guid.Parse("C4A91B3E-7D20-45F8-9B6A-2E8D51C30F74"));

      readonly Given_an_exactly_once_endpoint_declaring_its_domain_database_through_the_public_backend_registrar_extensions _specification;
      internal PublicApiDomainDatabaseEndpointDeclaration(Given_an_exactly_once_endpoint_declaring_its_domain_database_through_the_public_backend_registrar_extensions specification) => _specification = specification;

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireIntegrationTestTypeMappings();

      protected override void RegisterExactlyOnceTommandHandlers(IExactlyOnceTommandHandlerRegistrar handle) => handle
         .ForTommand((TommandProvingTheDeclaredPersistenceWorks _) =>
          {
             _specification._tommandHandled = true;
             return Task.CompletedTask;
          });
   }

   protected override async Task InitializeAsyncInternal() => await _host.StartAsync().caf();

   protected override async Task DisposeAsyncInternal() => await _host.DisposeAsync().caf();

   [PCT] public void the_endpoint_starts_and_runs() => _endpoint.IsRunning.Must().BeTrue();

   [PCT] public async Task a_tommand_the_endpoint_sends_itself_is_handled_through_the_extension_declared_persistence()
   {
      await _endpoint.ServiceLocator.Resolve<IIndependentTommandSender>().SendAsync(new TommandProvingTheDeclaredPersistenceWorks());
      _tommandHandled.Must().BeTrue();
   }

   protected internal class TommandProvingTheDeclaredPersistenceWorks : Remotable.ExactlyOnce.Tommand;
}
