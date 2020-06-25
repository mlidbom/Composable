using System;
using Composable.DDD;

namespace Composable.Persistence.EventStore
{
    //todo: make abstract
    public class AggregateEvent : ValueObject<AggregateEvent>, IAggregateEvent
    {
        protected AggregateEvent()
        {
            EventId = Guid.NewGuid();
            UtcTimeStamp = DateTime.UtcNow;//Todo:bug: Should use timesource.
        }

        protected AggregateEvent(Guid aggregateId) : this() => AggregateId = aggregateId;

        public Guid EventId { get; internal set; }
        public int AggregateVersion { get; internal set; }

        public Guid AggregateId { get; internal set; }
        public DateTime UtcTimeStamp { get; internal set; }
    }
}
