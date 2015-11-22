using System;
using System.Diagnostics.Contracts;
using Composable.DDD;

namespace Composable.CQRS.EventSourcing
{
    public class AggregateRootEvent : ValueObject<AggregateRootEvent>, IAggregateRootEvent
    {
        
        
        
        
        protected AggregateRootEvent()
        {
            EventId = Guid.NewGuid();
            UtcTimeStamp = DateTime.UtcNow;
        }

        protected AggregateRootEvent(Guid aggregateRootId) : this() { AggregateRootId = aggregateRootId; }

        //review:mlidbo: Fix the serialization issues with NServicebus and make sure that all the setters are private.
        public Guid EventId { get; set; }
        public int AggregateRootVersion { get; set; }

        public Guid AggregateRootId { get; set; }
        public DateTime UtcTimeStamp { get; set; }

        [Obsolete("Use UtcTimeStamp which is clear about what it is supposed to be. This property will be removed soon. It is only here to provide runtime compatibility")]
        public DateTime TimeStamp { get {return UtcTimeStamp;} set { UtcTimeStamp = value; } }

        internal int InsertedVersion { get; set; }
        internal int? EffectiveVersion { get; set; }
        internal int? ManualVersion { get; set; }

        internal long InsertionOrder { get; set; }
        internal long? Replaces { get; set; }

        internal long? InsertBefore { get; set; }

        internal long? InsertAfter { get; set; }
    }
}
