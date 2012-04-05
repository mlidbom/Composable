using System;
using Composable.DomainEvents;
using NServiceBus;

namespace Composable.CQRS.EventSourcing
{
    public interface IAggregateRootEvent : IEvent, IDomainEvent
    {
        Guid EventId { get; set; }
        int AggregateRootVersion { get; set; }        
        Guid AggregateRootId { get; set; }
        DateTime TimeStamp { get; set; }
    }
}