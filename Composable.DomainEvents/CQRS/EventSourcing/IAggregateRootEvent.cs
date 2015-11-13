using System;
using Composable.DomainEvents;
using NServiceBus;

namespace Composable.CQRS.EventSourcing
{
    public interface IAggregateRootEvent : IEvent, IDomainEvent
    {
        Guid EventId { get; }
        int AggregateRootVersion { get; }        
        Guid AggregateRootId { get; }
        DateTime TimeStamp { get; }
        long InsertionOrder { get; }
        long? Replaces { get; }
        long? InsertBefore { get; }
        long? InsertAfter { get; }
    }
}