using Compze.Tessaging.Endpoints;
using Compze.Tessaging._internal.SqlLayer;
using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging.TessageBus._private.Outbox;

// ReSharper disable once ClassCannotBeInstantiated rider is plain confused
partial class Outbox
{
   public interface ITessageStorage
   {
      ///<summary>Persists the tessage with one dispatching row per receiver, in the caller's save transaction — see<br/>
      /// <see cref="ITessagingSqlLayer.IOutboxSqlLayer.SaveTessageAsync"/>. Returns each receiver's assigned delivery stream<br/>
      /// sequence number, which the commit hook hands the connection's exactly-once stream.</summary>
      Task<IReadOnlyDictionary<EndpointId, long>> SaveTessageAsync(ITessage tessage, TessageId dedupId, params EndpointId[] receiverEndpointIds);

      ///<summary>The delivery attempt's declared predecessor — see<br/>
      /// <see cref="ITessagingSqlLayer.IOutboxSqlLayer.GetDeliveryStreamPredecessorSequenceNumberAsync"/>.</summary>
      Task<long> GetDeliveryStreamPredecessorSequenceNumberAsync(EndpointId receiverId, long sequenceNumber);
      Task MarkAsReceivedAsync(TessageId tessageId, EndpointId receiverId);
      Task RecordDeliveryFailureAsync(TessageId tessageId, EndpointId receiverId, Exception? exception);

      ///<summary>The endpoint's recovery backlog: every tessage bound to it and not yet received, in send order. Stranded<br/>
      /// tessages are excluded — a stranded tessage awaits explicit resolution<br/>
      /// (see <c>src/Compze.Tessaging/dev_docs/WIP/peer-administration.md</c>), never delivery.</summary>
      Task<IReadOnlyList<ITessagingSqlLayer.UndeliveredTessage>> GetUndeliveredTessagesForEndpointAsync(EndpointId endpointId);

      ///<summary>Marks these undelivered tessages bound to <paramref name="endpointId"/> stranded: kept and visible, but<br/>
      /// excluded from the recovery backlog until explicitly resolved — the fate of every undelivered tessage whose bound<br/>
      /// receiver's shrunk advertisement no longer serves its type.</summary>
      Task StrandUndeliveredTessagesAsync(EndpointId endpointId, IReadOnlyList<TessageId> tessageIds);

      Task StartAsync();
   }
}
