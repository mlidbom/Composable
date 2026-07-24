using Compze.Contracts;
using Compze.Tessaging.TessageBus.Exceptions;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.Discovery;
using Compze.Tessaging.Endpoints.Exceptions;
using Compze.Tessaging.Engine.HandlerRegistration;
using Compze.Tessaging.Peers._internal;
using Compze.Tessaging._private.Routing;
using Compze.Tessaging.TessageBus._private.TessageHandling.Dispatching;
using Compze.Tessaging.TessageTypes;
using Compze.Tessaging.Typermedia.Client;
using Compze.Threading;

using Compze.Tessaging._private.Transport.Advertisement;

namespace Compze.Tessaging._private.HandlerAvailability;

static class HandlerAvailabilityRegistrar
{
   internal static IComponentRegistrar HandlerAvailability(this IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<IHandlerAvailability>().CreatedBy(
            (ITessagingRouter router, IPeerRegistry peerRegistry, TessageHandlerRoster roster, HandlerAvailabilityPatience patience)
               => new HandlerAvailability(router, peerRegistry, roster, patience)));
}

#pragma warning disable CA1724 // Type name intentionally matches namespace concept
class HandlerAvailability : IHandlerAvailability
{
   readonly ITessagingRouter _router;
   readonly IPeerRegistry _peerRegistry;
   readonly TessageHandlerRoster _roster;
   readonly HandlerAvailabilityPatience _patience;

   internal HandlerAvailability(ITessagingRouter router, IPeerRegistry peerRegistry, TessageHandlerRoster roster, HandlerAvailabilityPatience patience)
   {
      _router = router;
      _peerRegistry = peerRegistry;
      _roster = roster;
      _patience = patience;
   }

   //Each wait's condition captures what its evaluations see, so an exhausted patience is classified from the same snapshot
   //whose evaluation exhausted it - a re-read after the wait could see the world having just become right and build a failure
   //message that lies about it. The condition is re-evaluated on every router state update and peer-advertisement recording
   //(see ITessagingRouter.TryAwaitConnectionsOrPeerMemorySatisfyingAsync), so nothing that becomes right within patience is
   //missed and nothing polls. A shutdown mid-wait needs no handling of its own: the router's lookups assert the router is not
   //stopped, and that failure propagates out of the wait immediately.

   public async Task<EndpointAddress> AwaitAddressOfTypermediaHandlerForAsync(Type tessageType)
   {
      IReadOnlyList<TypermediaRoute> liveRoutes = [];
      if(await _router.TryAwaitConnectionsOrPeerMemorySatisfyingAsync(() => (liveRoutes = _router.TypermediaRoutesFor(tessageType)).Count == 1, new WaitTimeout(_patience.Duration)).caf())
         return liveRoutes[0].Address;

      throw liveRoutes.Count == 0
               ? NoHandlerForTypermediaTypeException.BecausePatienceIsExhausted(tessageType, _patience.Duration, _peerRegistry.HandlerIdsFor(tessageType))
               : MultipleHandlersForTypermediaTypeException.BecausePatienceIsExhausted(tessageType, _patience.Duration, [..liveRoutes.Select(route => route.HandlerEndpointId)]);
   }

   public async Task<EndpointId> AwaitBindableReceiverOfAsync(Type tommandType)
   {
      EndpointId? bindableReceiverId = null;
      IReadOnlyList<EndpointId> rememberedHandlerIds = [];
      var aReceiverIsBindable = await _router.TryAwaitConnectionsOrPeerMemorySatisfyingAsync(() =>
      {
         //The same preference order the bind always had: the live handler - current by definition - over the sole remembered one.
         var liveConnection = _router.LiveConnectionToHandlerFor(tommandType);
         if(liveConnection != null)
         {
            bindableReceiverId = liveConnection.EndpointInformation.Id;
            return true;
         }

         rememberedHandlerIds = _peerRegistry.HandlerIdsFor(tommandType);
         if(rememberedHandlerIds.Count != 1) return false;
         bindableReceiverId = rememberedHandlerIds[0];
         return true;
      }, new WaitTimeout(_patience.Duration)).caf();

      if(aReceiverIsBindable) return bindableReceiverId._assert().NotNull();

      throw rememberedHandlerIds.Count == 0
               ? NoHandlerForTessageTypeException.BecausePatienceIsExhausted(tommandType, _patience.Duration)
               : MultipleHandlersForTessageTypeException.BecausePatienceIsExhausted(tommandType, _patience.Duration, rememberedHandlerIds);
   }

   public async Task AwaitHandlersForAsync(ReadinessTypes readinessTypes, TimeSpan? patience)
   {
      var effectivePatience = patience ?? _patience.Duration;
      List<Type> stillUnavailable = [];
      if(await _router.TryAwaitConnectionsOrPeerMemorySatisfyingAsync(
            () => (stillUnavailable = [..readinessTypes.Types.Where(tessageType => !AHandlerIsAvailableFor(tessageType))]).Count == 0,
            new WaitTimeout(effectivePatience)).caf())
         return;

      throw new EndpointNotReadyWithinPatienceException(
         $"The endpoint could still not reach a handler for every awaited type when its patience ran out (waited {effectivePatience.TotalSeconds:0.###}s). Still unavailable: "
         + string.Join("; ", stillUnavailable.Select(DescribeUnavailabilityOf)));
   }

   ///<summary>Whether a send of the single-handler type <paramref name="tessageType"/> would proceed without waiting: the<br/>
   /// endpoint's own roster serves it — reachable in-boundary, inline for tommands, the strictly-local navigator for<br/>
   /// typermedia — or, per the type's kind, an exactly-once tommand has a bindable receiver (a live handler, or the sole<br/>
   /// remembered one: known-but-down is served by the outbox waiting out the peer's absence) or a request/response type has<br/>
   /// exactly one live route.</summary>
   bool AHandlerIsAvailableFor(Type tessageType) =>
      _roster.HandlesTheSingleHandlerType(tessageType)
      || (tessageType.Is<IExactlyOnceTommand>()
             ? _router.LiveConnectionToHandlerFor(tessageType) != null || _peerRegistry.HandlerIdsFor(tessageType).Count == 1
             : _router.TypermediaRoutesFor(tessageType).Count == 1);

   string DescribeUnavailabilityOf(Type tessageType)
   {
      if(!tessageType.Is<IExactlyOnceTommand>() && _router.TypermediaRoutesFor(tessageType) is { Count: > 1 } ambiguousRoutes)
         return $"{tessageType.GetFullNameCompilable()} (more than one connected endpoint advertises it: {string.Join(", ", ambiguousRoutes.Select(route => route.HandlerEndpointId))})";

      var rememberedHandlerIds = _peerRegistry.HandlerIdsFor(tessageType);
      return rememberedHandlerIds.Count == 0
                ? $"{tessageType.GetFullNameCompilable()} (nothing this endpoint has ever met serves it)"
                : $"{tessageType.GetFullNameCompilable()} (remembered peers whose last-known advertisement serves it: {string.Join(", ", rememberedHandlerIds)}, none live)";
   }
}
