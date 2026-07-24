using Compze.Tessaging.Endpoints;
using Compze.DependencyInjection;
using Compze.Must;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.Testing;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.TessageTypes;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading;
using Compze.Threading.Testing;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.Tessaging;

///<summary>The cold-start half of waiting sends for exactly-once tommands: a send whose type's handler was never met does not<br/>
/// explode — it waits, within the endpoint's handler-availability patience, for the first contact, and only then binds to its<br/>
/// one specific receiver. The wait strictly precedes the bind, so the exactly-once in-order guarantee is untouched: the<br/>
/// tommand still binds exactly once, before its row is saved, and rides the bound pair's single ordered, receiver-deduped<br/>
/// delivery stream (see <c>src/Compze.Tessaging/dev_docs/peers.md</c>).</summary>
public class Given_an_exactly_once_tommand_send_racing_discovery : UniversalTestBase
{
   TestingEndpointHost _host = null!;
   ExactlyOnceEndpoint _senderEndpoint = null!;
   IDependencyInjectionContainer? _rootContainer;
   readonly IThreadGate _lateHandlerThreadGate = IThreadGate.NewOpen(WaitTimeout.Seconds(30), "LateHandlerThreadGate");
   readonly IThreadGate _retiredPeerThreadGate = IThreadGate.NewOpen(WaitTimeout.Seconds(30), "RetiredPeerThreadGate");

   protected override async Task InitializeAsyncInternal()
   {
      CreateHostWithTheSenderEndpoint();
      await _host.StartAsync();
   }

   protected override async Task DisposeAsyncInternal()
   {
      await _host.DisposeAsync();
      if(_rootContainer != null)
      {
         await _rootContainer.DisposeAsync();
         _rootContainer = null;
      }
   }

   [PCT] public async Task A_send_before_the_handling_endpoint_was_ever_met_waits_binds_on_first_contact_and_is_delivered()
   {
      //The send begins now: nothing this endpoint has ever met serves the type - the handling endpoint does not even exist yet.
      var sendTask = _senderEndpoint.ServiceLocator.Resolve<IIndependentTommandSender>().SendAsync(new MyExactlyOnceTommandHandledOnlyByTheLateEndpoint());

      await StartTheLateHandlerEndpointAsync();

      await sendTask;
      _lateHandlerThreadGate.AwaitPassedThroughCountEqualTo(1, WaitTimeout.Seconds(15));
   }

   [PCT] public async Task A_send_while_several_remembered_peers_advertise_the_type_and_none_is_live_binds_to_the_one_that_connects_and_is_delivered()
   {
      //The sender met the retired peer - a handler of the tommand's type - in one host generation, and the late handler in the
      //next: a handler replacement whose retired predecessor is still remembered. Its durable peer memory now remembers
      //two peers advertising the type...
      await MeetTheEndpointHandlingTheTommandTypeInItsOwnHostGenerationAsync(new RetiredPeerEndpointDeclaration(_retiredPeerThreadGate));
      await MeetTheEndpointHandlingTheTommandTypeInItsOwnHostGenerationAsync(new LateHandlerEndpointDeclaration(_lateHandlerThreadGate));
      //...and in this host generation neither of them is live: with no way to know which is current, the send waits instead of binding blind.
      await RebuildTheHostWithTheSenderEndpointAloneAsync();

      var sendTask = _senderEndpoint.ServiceLocator.Resolve<IIndependentTommandSender>().SendAsync(new MyExactlyOnceTommandHandledOnlyByTheLateEndpoint());

      //One of the remembered handlers connects: live is current by definition, so the send binds to it - the replacement
      //ambiguity is resolved by the very fact of connecting, and the retired peer's stream never sees the tommand.
      await StartTheLateHandlerEndpointAsync();

      await sendTask;
      _lateHandlerThreadGate.AwaitPassedThroughCountEqualTo(1, WaitTimeout.Seconds(15));
      _retiredPeerThreadGate.Passed.Must().Be(0);
   }

   void CreateHostWithTheSenderEndpoint()
   {
      //The root container shares the test's database pool across host generations: the sender endpoint keeps its identity and
      //thereby its database - and with it its durable peer memory - through every rebuild.
      _rootContainer ??= TestEnv.DIContainer.CreateTestingContainerBuilder()
                                ._mutate(it => it.Registrar.CurrentTestsDbPoolIfNotCloneContainer())
                                .Build();
      _host = TestingEndpointHost.Create(_rootContainer);
      _senderEndpoint = _host.RegisterEndpoint(new SenderEndpointDeclaration());
   }

   ///<summary>One host generation in which the sender meets <paramref name="handlerEndpoint"/> — an endpoint handling<br/>
   /// <see cref="MyExactlyOnceTommandHandledOnlyByTheLateEndpoint"/> — and remembers its advertisement durably.</summary>
   async Task MeetTheEndpointHandlingTheTommandTypeInItsOwnHostGenerationAsync(IExactlyOnceEndpointDeclaration handlerEndpoint)
   {
      await _host.DisposeAsync();
      CreateHostWithTheSenderEndpoint();
      _host.RegisterEndpoint(handlerEndpoint);
      await _host.StartAsync();
      await _host.AwaitEndpointsHaveMetEachOtherAsync();
   }

   async Task RebuildTheHostWithTheSenderEndpointAloneAsync()
   {
      await _host.DisposeAsync();
      CreateHostWithTheSenderEndpoint();
      await _host.StartAsync();
   }

   ///<summary>Composes and starts the endpoint handling <see cref="MyExactlyOnceTommandHandledOnlyByTheLateEndpoint"/> — after<br/>
   /// the host's phase barrier already ran, so the specification drives the late endpoint's own lifecycle phases: the same<br/>
   /// phases, in the same order. Its announcement is what the waiting send's patience is spent waiting for.</summary>
   async Task StartTheLateHandlerEndpointAsync()
   {
      var lateHandlerEndpoint = _host.RegisterEndpoint(new LateHandlerEndpointDeclaration(_lateHandlerThreadGate));
      await lateHandlerEndpoint.StartAsync();
   }

   class SenderEndpointDeclaration : ExactlyOnceEndpointDeclaration<SenderEndpointDeclaration>, IEndpointIdentity
   {
      public static string Name => "Sender";
      public static EndpointId Id => new(Guid.Parse("F1A83D57-2B96-4E40-8C15-7D3E90B62A84"));

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireIntegrationTestTypeMappings();
   }

   ///<summary>What the two tommand-handling endpoints share — everything but identity: the gated handler of<br/>
   /// <see cref="MyExactlyOnceTommandHandledOnlyByTheLateEndpoint"/>.</summary>
   abstract class TommandHandlingEndpointDeclaration<TIdentity> : ExactlyOnceEndpointDeclaration<TIdentity> where TIdentity : IEndpointIdentity
   {
      readonly IThreadGate _handlerGate;
      protected TommandHandlingEndpointDeclaration(IThreadGate handlerGate) => _handlerGate = handlerGate;

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireIntegrationTestTypeMappings();

      protected override void RegisterExactlyOnceTommandHandlers(IExactlyOnceTommandHandlerRegistrar handle) => handle
         .ForTommand((MyExactlyOnceTommandHandledOnlyByTheLateEndpoint _) =>
          {
             _handlerGate.AwaitPassThrough();
             return Task.CompletedTask;
          });
   }

   class RetiredPeerEndpointDeclaration : TommandHandlingEndpointDeclaration<RetiredPeerEndpointDeclaration>, IEndpointIdentity
   {
      public static string Name => "RetiredPeer";
      public static EndpointId Id => new(Guid.Parse("9E60B3A8-4C27-4D91-B7F5-2A85D1E04C36"));

      internal RetiredPeerEndpointDeclaration(IThreadGate handlerGate) : base(handlerGate) {}
   }

   class LateHandlerEndpointDeclaration : TommandHandlingEndpointDeclaration<LateHandlerEndpointDeclaration>, IEndpointIdentity
   {
      public static string Name => "LateHandler";
      public static EndpointId Id => new(Guid.Parse("58C2E9B4-71D0-4F36-A5E8-9B14F7C3D620"));

      internal LateHandlerEndpointDeclaration(IThreadGate handlerGate) : base(handlerGate) {}
   }
}

public class MyExactlyOnceTommandHandledOnlyByTheLateEndpoint : Remotable.ExactlyOnce.Tommand;
