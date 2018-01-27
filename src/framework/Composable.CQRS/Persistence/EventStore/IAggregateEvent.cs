using System;
using Composable.Messaging;

namespace Composable.Persistence.EventStore
{
    public interface IAggregateEvent : MessagingApi.Remote.ExactlyOnce.IEvent
    {
        Guid EventId { get; }
        int AggregateVersion { get; }
        Guid AggregateId { get; }
        DateTime UtcTimeStamp { get; }
    }
}
