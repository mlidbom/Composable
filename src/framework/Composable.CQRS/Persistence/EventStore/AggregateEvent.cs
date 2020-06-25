using System;
using Composable.DDD;
using Newtonsoft.Json;

namespace Composable.Persistence.EventStore
{
    //Review:mlidbo: Extract refactoring information into a separate abstraction so this one can be immutable.
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

        internal int InsertedVersion { get; set; }
        internal int? EffectiveVersion { get; set; }
        internal int? ManualVersion { get; set; }

        internal long InsertionOrder { get; set; }
        internal long? Replaces { get; set; }

        internal long? InsertBefore { get; set; }

        internal long? InsertAfter { get; set; }
    }
}
