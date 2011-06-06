using System;
using Newtonsoft.Json;

namespace Composable.CQRS.EventSourcing
{
    public class AggregateRootEvent : IAggregateRootEvent
    {
        protected AggregateRootEvent()
        {
            ((IAggregateRootEvent)this).EventId = Guid.NewGuid();
        }

        protected AggregateRootEvent(Guid aggregateRootId):this()
        {
            AggregateRootId = aggregateRootId;
        }

        public Guid EventId { get; set; }
        public int AggregateRootVersion { get; set; }
        public Guid AggregateRootId { get; set; }
    }
}