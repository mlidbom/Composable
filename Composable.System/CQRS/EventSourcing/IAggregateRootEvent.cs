using System;
using Composable.DomainEvents;
using Composable.Messaging;

namespace Composable.CQRS.EventSourcing
{
    public interface IAggregateRootEvent : IEvent, IDomainEvent
    {
        Guid EventId { get; }
        int AggregateRootVersion { get; }
        Guid AggregateRootId { get; }
        DateTime UtcTimeStamp { get; }
    }
}