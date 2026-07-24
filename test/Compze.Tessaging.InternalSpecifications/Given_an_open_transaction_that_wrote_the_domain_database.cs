using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Must;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Peers._internal;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.TessageBus._internal;
using Compze.Tessaging.TessageTypes;
using Compze.Tessaging._internal.SqlLayer;
using Compze.Tessaging._internal.Transport;
using Compze.Tessaging._private.Transport.Advertisement;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.TypeIdentifiers;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tessaging.InternalSpecifications;

///<summary>Recording a peer's advertisement is what satisfies a handler's waiting send: a handling transaction can sit inside<br/>
/// its open unit of work awaiting exactly the peer knowledge the recording provides, so the recording must complete while such<br/>
/// a transaction is open. On SQLite that is a real constraint: an open transaction that wrote the domain database holds the<br/>
/// database's whole write gate (<c>CompzeSqliteConnection</c>), and the recording completes despite it only because it never<br/>
/// needs that gate — its reads run unenlisted, and its save commits to the peer registry's own database<br/>
/// (<c>ISqlitePeerRegistryConnectionPool</c>). This specification pins that: the recording is awaited <em>inside</em> an open<br/>
/// transaction that wrote the domain database, so were the recording ever again to queue behind the domain database's write<br/>
/// path, it would deadlock here until the timeout fails the specification.</summary>
public class Given_an_open_transaction_that_wrote_the_domain_database : UniversalTestBase
{
   static readonly EndpointId NeverMetPeerId = new(Guid.Parse("CEA2F22C-1E90-438D-9318-5A46774ABF9A"));
   static readonly EndpointId AdmittedTommandsSenderPeerId = new(Guid.Parse("0599BC53-7A41-461C-968B-37C7D73F9B75"));

   readonly TestingEndpointHost _host;
   readonly ExactlyOnceEndpoint _endpoint;

   public Given_an_open_transaction_that_wrote_the_domain_database()
   {
      _host = TestingEndpointHost.Create();
      _endpoint = _host.RegisterEndpoint(new RecordingEndpointDeclaration());
   }

   class RecordingEndpointDeclaration : ExactlyOnceEndpointDeclaration<RecordingEndpointDeclaration>, IEndpointIdentity
   {
      public static string Name => "RecordingEndpoint";
      public static EndpointId Id => new(Guid.Parse("ADAF11CA-4BF6-45AB-9BD7-6AD00D8AD02B"));

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireTessagingInternalSpecificationTypeMappings();
   }

   protected override async Task InitializeAsyncInternal() => await _host.StartAsync();
   protected override async Task DisposeAsyncInternal() => await _host.DisposeAsync();

   [PCT] public async Task recording_a_never_met_peers_advertisement_completes_while_the_transaction_is_still_open()
   {
      var inbox = _endpoint.ServiceLocator.Resolve<ITessagingSqlLayer.IInboxSqlLayer>();
      var peerRegistry = _endpoint.ServiceLocator.Resolve<IPeerRegistry>();

      var tommand = new MyExactlyOnceTommandWhoseAdmissionIsTheOpenTransactionsWrite();
      var serializedTommand = _endpoint.ServiceLocator.Resolve<ITessagingSerializer>().SerializeTessage(tommand);
      var tommandTypeId = _endpoint.ServiceLocator.Resolve<ITypeMap>().GetId(tommand.GetType());

      await TransactionScopeCe.ExecuteAsync(async () =>
      {
         //The transaction's write: an inbox admission, the same kind of write a handling transaction's claim performs - on
         //sqlite the first write is what takes the domain database's write gate, held until this scope completes.
         (await inbox.SaveTessageAsync(tommand.Id, tommandTypeId, serializedTommand,
                                       new DeliveryStreamPosition(AdmittedTommandsSenderPeerId, sequenceNumber: 1, predecessorSequenceNumber: 0)))
           .Must().Be(ITessagingSqlLayer.SaveTessageResult.NewTessage);

         //Awaited inside the still-open scope: a recording that queued behind the domain database's write path could never
         //complete before this scope does, so it would burn the whole timeout and fail the specification.
         await peerRegistry.RecordAdvertisementAsync(new EndpointInformation("NeverMetPeer", NeverMetPeerId, []))
                           .WaitAsync(TimeSpan.FromSeconds(10));
      });

      peerRegistry.Peers.Single().Id.Must().Be(NeverMetPeerId);
   }
}

public class MyExactlyOnceTommandWhoseAdmissionIsTheOpenTransactionsWrite : Remotable.ExactlyOnce.Tommand;
