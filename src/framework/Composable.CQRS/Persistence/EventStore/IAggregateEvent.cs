using System;
using Composable.Messaging;
using Newtonsoft.Json;

namespace Composable.Persistence.EventStore
{
    public interface IAggregateEvent : MessageTypes.Remotable.ExactlyOnce.IEvent
    {
        Guid EventId { get; }
        int AggregateVersion { get; }
        Guid AggregateId { get; }
        DateTime UtcTimeStamp { get; }

#pragma warning disable CA1033 // Interface methods should be callable by child types
        //Todo: having this here feels icky. Why both EventId and MessageID? Can't we make this cleaner?
        [JsonIgnore] Guid MessageTypes.Remotable.IAtMostOnceMessage.MessageId => EventId;
#pragma warning restore CA1033 // Interface methods should be callable by child types
    }
}
