using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Tessaging.Endpoints;

namespace Compze.Tessaging.TessageBus._private.TessageHandling.Dispatching;

///<summary>Thrown when a tommand is sent while more than one remembered peer advertises handling its type and none of them is<br/>
/// live — a handler replacement whose retired peer is still remembered. A tommand binds to one specific receiver at send<br/>
/// time so the pair's exactly-once in-order guarantee holds; with several remembered handlers and no live one there is no way<br/>
/// to know which is current, and binding to the wrong one would strand the tommand (see<br/>
/// <c>src/Compze.Tessaging/dev_docs/peers.md</c>). Exclusively<br/>
/// the patience-exhausted failure: the send first waited, bounded by the endpoint's handler-availability patience, for one of<br/>
/// the remembered handlers to connect — live is current by definition, so the moment one does the send binds to it<br/>
/// (see <c>IHandlerAvailability</c>). Removing a retired peer from memory awaits the peer-administration design<br/>
/// (<c>src/Compze.Tessaging/dev_docs/WIP/peer-administration.md</c>). Public because it reaches the sending<br/>
/// application code, which must be able to catch it.</summary>
class MultipleHandlersForTessageTypeException : Exception
{
   MultipleHandlersForTessageTypeException(string message) : base(message) {}

   internal static MultipleHandlersForTessageTypeException BecausePatienceIsExhausted(Type tommandType, TimeSpan patience, IReadOnlyList<EndpointId> rememberedHandlerIds) => new(
      $"More than one remembered peer advertises handling the tommand type {tommandType.GetFullNameCompilable()} ({string.Join(", ", rememberedHandlerIds)}), none of them is live, and none connected within the endpoint's handler-availability patience (waited {patience.TotalSeconds:0.###}s). "
      + "A tommand binds to one specific receiver at send time, and with several remembered handlers there is no way to know which is current. This happens after a handler replacement whose retired endpoint is still remembered: bring the current handler up, so the send binds to the live one.");
}
