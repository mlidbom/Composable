using System;
using Composable.DDD;

namespace Composable.CQRS.EventSourcing
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

        protected AggregateRootEvent(Guid aggregateRootId) : this() { AggregateRootId = aggregateRootId; }

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

        public Guid EventId { get; set; }
        public int AggregateRootVersion { get; set; }

        public Guid AggregateRootId { get; set; }
        public DateTime UtcTimeStamp { get; set; }

        public int InsertedVersion { get; internal set; }
        public int? EffectiveVersion { get; internal set; }
        public int? ManualVersion { get; internal set; }

        public long InsertionOrder { get; internal set; }
        public long? Replaces { get; internal set; }

        public long? InsertBefore { get; internal set; }

        public long? InsertAfter { get; internal set; }
    }
}
