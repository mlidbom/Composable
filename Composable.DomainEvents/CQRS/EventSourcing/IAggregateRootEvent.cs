using System;
using Composable.DomainEvents;
using NServiceBus;

namespace Composable.CQRS.EventSourcing
{
    //Review:mlidbo: Event interfaces should never have setters. Refactor to make the interface immutable. Possible except for some obsoleted method with dire warnings and ridiculous naming to show that it is forbidden to use except within the implementation here.
    public interface IAggregateRootEvent : IEvent, IDomainEvent
    {
        Guid EventId { get; set; }
        int AggregateRootVersion { get; set; }        
        Guid AggregateRootId { get; set; }
        DateTime TimeStamp { get; set; }
        int InsertionOrder { get; set; }
    }
}