using System.Collections.Concurrent;
using System.Transactions;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting;
using Compze.Hosting.SameMachine;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Underscore;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.SystemCE.TransactionsCE.Testing;
using Compze.Internals.Testing;
using Compze.Internals.Testing.Utilities.Awaiting;
using Compze.Must;
using Compze.Tessaging._private.Routing;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.BestEffort;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.TessageBus.Exceptions;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading;
using Compze.Threading.Testing;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tessaging.InternalSpecifications;

///<summary>
/// Queue-while-down on the distributed tier (see <c>src/Compze.Tessaging/dev_docs/peers.md</c>): a publisher that has met a
/// subscribing peer keeps its tevents for it while the peer is down — in memory, in order, nothing persisted anywhere — and the
/// peer's next connection drains them on its return. The subscriber lives in its own host so it can go down and return while
/// the publisher stays up; identity is the subscriber's <see cref="EndpointId"/>, which its next incarnation keeps — one
/// declaration, several endpoint instances across the incarnations.
///</summary>
///<remarks>An internal specification because "down" is a fact about the publisher's router, not about anything a consumer can
/// observe: queueing begins when the connection drops and draining begins when it returns, so every step here has to wait for
/// <see cref="ITessagingRouter.HasLiveConnectionTo"/> to say so. A consumer never asks that — waiting sends, <c>RequirePeers</c>
/// and readiness exist precisely so an application never has to — and its predecessor among the black-box specifications proved
/// what pretending costs: it waited instead on the peer being <em>remembered</em>, which is true before the connection exists.</remarks>
public class Given_a_met_distributed_tessaging_subscriber_that_goes_down : UniversalTestBase
{
   static readonly WaitTimeout HandlerTimeout = WaitTimeout.Seconds(30);
   static DirectoryInfo TestDirectory => new DirectoryInfo(Path.Combine(Path.GetTempPath(), "Compze", "Tests", "EndpointRegistry"))._mutate(it => it.Create());

   readonly InterprocessEndpointRegistry _registry;
   readonly IEndpointHost _publisherHost;
   readonly BestEffortEndpoint _publisherEndpoint;
   IEndpointHost _subscriberHost;

   //Shared across the subscriber's incarnations: whichever incarnation receives a tevent records it here, so the assertion reads
   //one uninterrupted sequence across the downtime.
   readonly IThreadGate _subscriberTeventHandlerGate = IThreadGate.NewOpen(HandlerTimeout, "subscriberBestEffortTeventHandler");
   readonly ConcurrentQueue<ISequencedBestEffortTevent> _teventsHandledOnTheSubscriber = new();

   public Given_a_met_distributed_tessaging_subscriber_that_goes_down()
   {
      _registry = InterprocessEndpointRegistry.OpenOrCreateSessionLocal(Guid.NewGuid().ToString(), TestDirectory);

      _publisherHost = EndpointHost.Production.Create(() => TestEnv.DIContainer.CreateTestingContainerBuilder(), new EnvironmentParticipatingInTheSpecificationsRegistry(_registry));
      _publisherEndpoint = _publisherHost.RegisterEndpoint(new PublisherEndpointDeclaration());

      _subscriberHost = CreateSubscriberHost();
   }

   IEndpointHost CreateSubscriberHost()
   {
      var host = EndpointHost.Production.Create(() => TestEnv.DIContainer.CreateTestingContainerBuilder(), new EnvironmentParticipatingInTheSpecificationsRegistry(_registry));
      host.RegisterEndpoint(new SubscriberEndpointDeclaration(this));
      return host;
   }

   class PublisherEndpointDeclaration : BestEffortEndpointDeclaration<PublisherEndpointDeclaration>, IEndpointIdentity
   {
      public static string Name => "QueueWhileDownPublisherEndpoint";
      public static EndpointId Id { get; } = new(Guid.Parse("21C2E6A4-9B0F-4C3D-8A57-3F1DE08B6C92"));

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireTessagingInternalSpecificationTypeMappings();
   }

   class SubscriberEndpointDeclaration : BestEffortEndpointDeclaration<SubscriberEndpointDeclaration>, IEndpointIdentity
   {
      public static string Name => "QueueWhileDownSubscriberEndpoint";
      public static EndpointId Id { get; } = new(Guid.Parse("7B7E51D5-2E1A-4E0C-9C11-6C0F5B1D2A43"));

      readonly Given_a_met_distributed_tessaging_subscriber_that_goes_down _specification;
      internal SubscriberEndpointDeclaration(Given_a_met_distributed_tessaging_subscriber_that_goes_down specification) => _specification = specification;

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireTessagingInternalSpecificationTypeMappings();

      protected override void RegisterBestEffortTeventHandlers(IBestEffortTeventHandlerRegistrar handle) => handle
         .ForTevent((ISequencedBestEffortTevent tevent) =>
          {
             _specification._teventsHandledOnTheSubscriber.Enqueue(tevent);
             _specification._subscriberTeventHandlerGate.AwaitPassThrough();
          });
   }

   protected override async Task InitializeAsyncInternal()
   {
      await _publisherHost.StartAsync().caf();
      await _subscriberHost.StartAsync().caf();
   }

   protected override async Task DisposeAsyncInternal()
   {
      await _publisherHost.DisposeAsync().caf();
      await _subscriberHost.DisposeAsync().caf();
      _registry.Delete();
      _registry.Dispose();
   }

   [PCT] public async Task tevents_published_while_the_subscriber_is_down_are_delivered_in_order_when_it_returns()
   {
      await MeetTheSubscriberDeliveringTeventOneThenTakeItDownAsync();

      2.Through(4).ForEach(PublishOnThePublisherEndpointInATransaction);

      //Return: a new incarnation of the subscriber - same EndpointId, new process-equivalent host - and the queue drains to it.
      _subscriberHost = CreateSubscriberHost();
      await _subscriberHost.StartAsync();

      _subscriberTeventHandlerGate.AwaitPassedThroughCountEqualTo(4);
      _teventsHandledOnTheSubscriber.Select(it => it.SequenceNumber).SequenceEqual(1.Through(4)).Must().BeTrue();
   }

   [PCT] public async Task the_publish_that_would_exceed_10_000_queued_tevents_for_the_down_subscriber_fails_loud_naming_the_peer_and_the_bound()
   {
      await MeetTheSubscriberDeliveringTeventOneThenTakeItDownAsync();

      _publisherEndpoint.ServiceLocator.Resolve<IScopeFactory>().ExecuteUnitOfWork(unitOfWork =>
      {
         var publisher = unitOfWork.Resolve<IUnitOfWorkTeventPublisher>();
         //Slots are reserved at publish, inside the transaction, so the bound is hit before anything commits: these
         //10,000 publishes fill the subscriber's queue bound exactly...
         2.Through(10_001).ForEach(sequenceNumber => publisher.Publish(new SequencedBestEffortTevent { SequenceNumber = sequenceNumber }));

         //...and the publish that would exceed it fails loud - backpressure naming the peer and the bound, never silent shedding.
         var message = Invoking(() => publisher.Publish(new SequencedBestEffortTevent { SequenceNumber = 10_002 }))
                      .Must().Throw<BestEffortTeventQueueOverflowException>()
                      .Which.Message;
         message.Must().Contain(SubscriberEndpointDeclaration.Id.ToString());
         message.Must().Contain("10000");
      });
   }

   [PCT] public async Task a_unit_of_work_that_rolls_back_releases_the_queue_slots_its_publishes_reserved()
   {
      await MeetTheSubscriberDeliveringTeventOneThenTakeItDownAsync();

      //A unit of work reserving the subscriber's entire queue bound rolls back...
      Invoking(() => _publisherEndpoint.ServiceLocator.Resolve<IScopeFactory>().ExecuteUnitOfWork(unitOfWork =>
                    {
                       Transaction.Current!.FailOnPrepare();
                       var publisher = unitOfWork.Resolve<IUnitOfWorkTeventPublisher>();
                       2.Through(10_001).ForEach(sequenceNumber => publisher.Publish(new SequencedBestEffortTevent { SequenceNumber = sequenceNumber }));
                    }))
                   .Must().Throw<TransactionAbortedException>();

      //...and its reservations die with it: a fresh unit of work can fill the whole bound again.
      _publisherEndpoint.ServiceLocator.Resolve<IScopeFactory>().ExecuteUnitOfWork(unitOfWork =>
      {
         var publisher = unitOfWork.Resolve<IUnitOfWorkTeventPublisher>();
         2.Through(10_001).ForEach(sequenceNumber => publisher.Publish(new SequencedBestEffortTevent { SequenceNumber = sequenceNumber }));
      });
   }

   ///<summary>The shared given: the publisher meets the subscriber (proven by tevent 1 arriving), then the subscriber goes<br/>
   /// down — it retracts its announcement while still serving, so the publisher's router drops the connection cleanly and no<br/>
   /// delivery can be in flight at a failure moment; only then does the subscriber's host go away.</summary>
   async Task MeetTheSubscriberDeliveringTeventOneThenTakeItDownAsync()
   {
      AwaitThePublisherConnectedToTheSubscriber();
      PublishOnThePublisherEndpointInATransaction(sequenceNumber: 1);
      _subscriberTeventHandlerGate.AwaitPassedThroughCountEqualTo(1);

      _registry.RetractEndpointAddress(SubscriberEndpointDeclaration.Id);
      AwaitThePublisherDisconnectedFromTheSubscriber();
      await _subscriberHost.DisposeAsync();
   }

   void PublishOnThePublisherEndpointInATransaction(int sequenceNumber) =>
      _publisherEndpoint.ServiceLocator.Resolve<IScopeFactory>().ExecuteUnitOfWork(unitOfWork =>
         unitOfWork.Resolve<IUnitOfWorkTeventPublisher>().Publish(new SequencedBestEffortTevent { SequenceNumber = sequenceNumber }));

   ///<summary>Queueing begins when the connection drops and draining when it returns, so each step waits for the connection to
   /// be in the state that step is about.</summary>
   void AwaitThePublisherConnectedToTheSubscriber() =>
      PublishersRouter.PollAwait(it => it.HasLiveConnectionTo(SubscriberEndpointDeclaration.Id));

   void AwaitThePublisherDisconnectedFromTheSubscriber() =>
      PublishersRouter.PollAwait(it => !it.HasLiveConnectionTo(SubscriberEndpointDeclaration.Id));

   ITessagingRouter PublishersRouter => _publisherEndpoint.ServiceLocator.Resolve<ITessagingRouter>();
}
