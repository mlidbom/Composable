using System;
using Composable.DDD;

namespace Composable.CQRS.EventSourcing
{
    //TODO: Not sure about making valueobject the base class, but about a gazillion tests in some parts of the code apparently depends on several subclasses being ValueObjects
    public class AggregateRootEvent : ValueEqualityEvent<AggregateRootEvent>, IAggregateRootEvent
    {
        protected AggregateRootEvent()
        {
            EventId = Guid.NewGuid();
            TimeStamp = DateTime.UtcNow;
        }

        protected AggregateRootEvent(Guid aggregateRootId)
            : this()
        {
            AggregateRootId = aggregateRootId;
        }

        public Guid EventId { get; set; }
        public int AggregateRootVersion { get; set; }
        public Guid AggregateRootId { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}