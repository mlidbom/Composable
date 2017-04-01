using System;
using Composable.Messaging;

namespace Composable.Persistence.EventStore
{
    public interface IAggregateRootEvent : IEvent
    {
        Guid EventId { get; }
        int AggregateRootVersion { get; }
        Guid AggregateRootId { get; }
        DateTime UtcTimeStamp { get; }
    }
}