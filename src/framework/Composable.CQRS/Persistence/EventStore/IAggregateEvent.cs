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
        [JsonIgnore] Guid MessageTypes.Remotable.IAtMostOnceMessage.DeduplicationId => EventId;
    }
}
