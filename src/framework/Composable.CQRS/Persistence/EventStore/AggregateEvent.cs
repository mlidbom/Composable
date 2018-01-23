using System;
using Composable.DDD;
using Newtonsoft.Json;

namespace Composable.Persistence.EventStore
{
    //Review:mlidbo: Make instances immutable: Inspect inheriting types and throw exception if mutable.
    //Review:mlidbo: Extract refactoring information into a separate abstraction.
    //todo: make abstract
    public class AggregateEvent : ValueObject<AggregateEvent>, IAggregateEvent
    {
        protected AggregateEvent()
        {
            EventId = Guid.NewGuid();
            UtcTimeStamp = DateTime.UtcNow;//Todo: Should use timesource.
        }

        protected AggregateEvent(Guid aggregateRootId) : this() => AggregateId = aggregateRootId;

        [Obsolete("Only intended for testing. Do not use for normal inheritance.")] protected AggregateEvent(Guid? eventId = null,
                                                                                                                 int? aggregateRootVersion = null,
                                                                                                                 Guid? aggregateRootId = null,
                                                                                                                 DateTime? utcTimeStamp = null,
                                                                                                                 int? insertedVersion = null,
                                                                                                                 int? effectiveVersion = null,
                                                                                                                 int? manualVersion = null,
                                                                                                                 long? insertionOrder = null,
                                                                                                                 long? replaces = null,
                                                                                                                 long? insertBefore = null,
                                                                                                                 long? insertAfter = null)
        {
            EventId = eventId ?? EventId;
            AggregateVersion = aggregateRootVersion ?? AggregateVersion;
            AggregateId = aggregateRootId ?? AggregateId;
            UtcTimeStamp = utcTimeStamp ?? UtcTimeStamp;
            InsertedVersion = insertedVersion ?? InsertedVersion;
            EffectiveVersion = effectiveVersion;
            ManualVersion = manualVersion;
            InsertionOrder = insertionOrder ?? InsertionOrder;
            Replaces = replaces;
            InsertBefore = insertBefore;
            InsertAfter = insertAfter;
        }

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

        [JsonIgnore]
        public Guid MessageId => EventId;
    }
}
