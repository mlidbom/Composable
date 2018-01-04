using System;
using Composable.Messaging;

namespace Composable.Persistence.EventStore
{
    public interface IAggregateRootEvent : ITransactionalExactlyOnceDeliveryEvent
    {
        Guid EventId { get; }
        int AggregateRootVersion { get; }
        Guid AggregateRootId { get; }
        DateTime UtcTimeStamp { get; }
    }
}
