using System;
using Composable.DomainEvents;

namespace Composable.CQRS.EventSourcing
{
    public interface IAggregateRootEvent : IDomainEvent
    {
        Guid Id { get; set; }
        int AggregateRootVersion { get; set; }        
        Guid AggregateRootId { get; set; }
    }
}