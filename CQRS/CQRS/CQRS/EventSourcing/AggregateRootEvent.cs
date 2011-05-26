using System;

namespace Composable.CQRS.EventSourcing
{
    public class AggregateRootEvent : IAggregateRootEvent
    {
        protected AggregateRootEvent()
        {
            Id = Guid.NewGuid();
        }
        public int Version { get; set; }
        public Guid Id { get; set; }
        public Guid EntityId { get; set; }
    }
}