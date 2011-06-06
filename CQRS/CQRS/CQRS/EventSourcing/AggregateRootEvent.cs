using System;
using Composable.DDD;
using Newtonsoft.Json;

namespace Composable.CQRS.EventSourcing
{
    //TODO: Not sure about making valueobject the base class, but about a gazillion tests in some parts of the code apparently depends on several subclasses being ValueObjects
    public class AggregateRootEvent : ValueObject<AggregateRootEvent>, IAggregateRootEvent
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