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
        DateTime UtcTimeStamp { get; }

        [Obsolete("Please use UtcTimeStamp which is clear about what it is supposed to be. This propert will be removed soon. It is only here to provide runtime compatibility", error: true)]
        DateTime TimeStamp { get; }
    }
}