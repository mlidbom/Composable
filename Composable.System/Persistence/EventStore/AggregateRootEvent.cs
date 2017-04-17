using System;
using Composable.DDD;

namespace Composable.Persistence.EventStore
{
    //review:mlidbo: Make instances immutable.
    //Review:mlidbo: Extract refactoring information into a separate abstraction?
    public class AggregateRootEvent : ValueObject<AggregateRootEvent>, IAggregateRootEvent
    {
        protected AggregateRootEvent()
        {
            EventId = Guid.NewGuid();
            UtcTimeStamp = DateTime.UtcNow;
        }

        protected AggregateRootEvent(Guid aggregateRootId) : this() => AggregateRootId = aggregateRootId;

        [Obsolete("Only intended for testing. Do not use for normal inheritance.")] protected AggregateRootEvent(Guid? eventId = null,
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
            AggregateRootVersion = aggregateRootVersion ?? AggregateRootVersion;
            AggregateRootId = aggregateRootId ?? AggregateRootId;
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
        public int AggregateRootVersion { get; internal set; }

        public Guid AggregateRootId { get; internal set; }
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
