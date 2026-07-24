using Compze.Tessaging.Endpoints;

namespace Compze.Tessaging._internal.Transport;

///<summary>Where an exactly-once tessage sits in its delivery stream. Every tessage between a sender endpoint and a receiver<br/>
/// endpoint rides that pair's single delivery stream, and this is the tessage's coordinates in it: which stream — named by<br/>
/// <see cref="SenderEndpointId"/>, since the receiving endpoint completes the pair — the tessage's own<br/>
/// <see cref="SequenceNumber"/>, and its <see cref="PredecessorSequenceNumber"/>: the stream member the receiving inbox must<br/>
/// have admitted before this one.</summary>
///<remarks>The sequence number is assigned inside the transaction that saves the tessage to the sender's outbox, from a<br/>
/// per-receiver counter row whose lock serializes the pair's commits — so sequence order is commit order, whatever<br/>
/// interleaving the sending transactions had. The stream can acquire holes after assignment: sender-side stranding parks an<br/>
/// undelivered tessage whose type the receiver's shrunk advertisement renounced, and its sequence number then never<br/>
/// arrives. So each delivery attempt declares its predecessor — the largest lower sequence number still deliverable or<br/>
/// already received, freshly computed from the sender's durable dispatching rows — and the receiving inbox admits a tessage<br/>
/// only when its admission high-water mark equals that declared predecessor, refusing any other (redeliveries at or below the<br/>
/// mark excepted, as duplicates). That is what makes exactly-once in-order delivery hold by construction at the receiver<br/>
/// instead of resting on invariants about sender behavior.</remarks>
class DeliveryStreamPosition
{
   internal EndpointId SenderEndpointId { get; }
   internal long SequenceNumber { get; }

   ///<summary>The sequence number of the pair's previous still-deliverable-or-received stream member at this delivery<br/>
   /// attempt — 0 when this tessage leads the stream. The receiving inbox admits this tessage iff its admission<br/>
   /// high-water mark equals this value.</summary>
   internal long PredecessorSequenceNumber { get; }

   internal DeliveryStreamPosition(EndpointId senderEndpointId, long sequenceNumber, long predecessorSequenceNumber)
   {
      SenderEndpointId = senderEndpointId;
      SequenceNumber = sequenceNumber;
      PredecessorSequenceNumber = predecessorSequenceNumber;
   }

   public override string ToString() => $"sequence number {SequenceNumber} (declared predecessor {PredecessorSequenceNumber}) in the delivery stream from sender {SenderEndpointId.Value}";
}
