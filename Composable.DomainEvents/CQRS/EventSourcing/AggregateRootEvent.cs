using System;
using Composable.DDD;

namespace Composable.CQRS.EventSourcing
{
    //TODO: Not sure about making value object the base class, but about a gazillion tests in some parts of the code depends on several subclasses being ValueObjects
    public class AggregateRootEvent : ValueObject<AggregateRootEvent>, IAggregateRootEvent
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

        //review:mlidbo: Fix the serialization issues with NServicebus and make sure that all the setters are private.
        public Guid EventId { get; set; }
        public int AggregateRootVersion { get; set; }
        public Guid AggregateRootId { get; set; }
        public DateTime TimeStamp { get; set; }
        public long InsertionOrder { get; set; }
        public long? Replaces { get; set; }
        public long? InsertBefore { get; set; }
        public long? InsertAfter { get; set; }
    }
}