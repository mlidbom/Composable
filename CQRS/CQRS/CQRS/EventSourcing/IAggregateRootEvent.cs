using System;
using System.Data;
using Composable.DomainEvents;
using NServiceBus;

namespace Composable.CQRS.EventSourcing
{
    public interface IAggregateRootEvent : IMessage, IDomainEvent
    {
        Guid EventId { get; set; }
        int AggregateRootVersion { get; set; }        
        Guid AggregateRootId { get; set; }
        DateTime TimeStamp { get; set; }
    }
}