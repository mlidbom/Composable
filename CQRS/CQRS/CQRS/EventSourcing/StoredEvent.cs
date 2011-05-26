using System;
using Composable.DomainEvents;

namespace Composable.CQRS.EventSourcing
{
    public class StoredEvent
    {
        public StoredEvent(IAggregateRootEvent @event)
        {
            Id = @event.Id;
            Event = @event;
        }

        public Guid Id { get; set; }

        public IAggregateRootEvent Event { get; set; }
    }
}