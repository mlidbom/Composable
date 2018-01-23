using System;
using Composable.Messaging;

namespace Composable.Persistence.EventStore
{
    public interface IAggregateEvent : ITransactionalExactlyOnceDeliveryEvent
    {
        Guid EventId { get; }
        int AggregateVersion { get; }
        Guid AggregateId { get; }
        DateTime UtcTimeStamp { get; }
    }
}
