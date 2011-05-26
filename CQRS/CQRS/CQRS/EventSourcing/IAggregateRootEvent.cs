using System;
using Composable.DomainEvents;

namespace Composable.CQRS.EventSourcing
{
    public interface IAggregateRootEvent : IDomainEvent
    {
        int Version { get; set; }
        Guid Id { get; set; }
        Guid EntityId { get; set; }
    }
}