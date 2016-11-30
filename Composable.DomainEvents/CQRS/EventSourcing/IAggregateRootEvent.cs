using System;
using Composable.DomainEvents;

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