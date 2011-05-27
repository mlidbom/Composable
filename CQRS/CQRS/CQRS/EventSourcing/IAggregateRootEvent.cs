using System;
using Composable.DomainEvents;

namespace Composable.CQRS.EventSourcing
{
    public interface IAggregateRootEvent : IDomainEvent
    {
        Guid EventId { get; set; }
        int AggregateRootVersion { get; set; }        
        Guid AggregateRootId { get; set; }
    }
}