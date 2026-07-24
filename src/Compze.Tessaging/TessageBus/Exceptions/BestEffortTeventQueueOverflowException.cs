using Compze.Tessaging.TessageBus._private.BestEffortDelivery;
using Compze.Tessaging.Endpoints;

namespace Compze.Tessaging.TessageBus.Exceptions;

///<summary>Thrown by a best-effort tevent publish when the tevent would exceed its peer's queue bound<br/>
/// (<see cref="BestEffortTeventQueues.MaximumQueuedTeventsPerPeer"/>). The queue grows while a remembered subscriber is down,<br/>
/// so hitting the bound means the peer has been down — or unable to keep up — for a long time. This is deliberate backpressure:<br/>
/// failing the publish loud, inside the caller's transaction, loses nothing, while silently shedding queued tevents does<br/>
/// (see <c>src/Compze.Tessaging/dev_docs/peers.md</c>). Removing a peer that is gone for good from the endpoint's memory<br/>
/// awaits the peer-administration design (see <c>src/Compze.Tessaging/dev_docs/WIP/peer-administration.md</c>).</summary>
public class BestEffortTeventQueueOverflowException : Exception
{
   internal BestEffortTeventQueueOverflowException(EndpointId peerId) : base(
      $"Cannot queue another best-effort tevent for peer {peerId}: its queue is at the bound of {BestEffortTeventQueues.MaximumQueuedTeventsPerPeer} tevents (queued, plus reserved by uncommitted transactions). " +
      "The peer has been down, or unable to keep up, for long enough to exhaust the bound. The publish fails rather than tevents being silently shed.") {}
}
