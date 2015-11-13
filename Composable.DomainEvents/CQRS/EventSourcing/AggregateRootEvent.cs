using System;
using System.Diagnostics.Contracts;
using Composable.DDD;

namespace Composable.CQRS.EventSourcing
{
    public class AggregateRootEvent : ValueObject<AggregateRootEvent>, IAggregateRootEvent
    {
        private long? _replaces;
        private long? _insertBefore;
        private long? _insertAfter;
        private long _insertionOrder;
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
        public int InsertedVersion { get; set; }
        public int? EffectiveVersion { get; set; }
        public int? ManualVersion { get; set; }

        public Guid AggregateRootId { get; set; }
        public DateTime TimeStamp { get; set; }

        public long InsertionOrder
        {
            get
            {
                return _insertionOrder;
            }
            set
            {
                Contract.Requires(value > 0);

                if(value <= 0)
                {
                    throw new Exception("wtf");
                }
                _insertionOrder = value;
            }
        }

        public long? Replaces
        {
            get
            {
                Contract.Ensures(Contract.Result<long?>() > 0 || Contract.Result<long?>() == null);
                return _replaces;
            }
            set
            {
                Contract.Requires(value > 0 || value == null);
                _replaces = value;
            }
        }

        public long? InsertBefore
        {
            get
            {
                Contract.Ensures(Contract.Result<long?>() > 0 || Contract.Result<long?>() == null);
                return _insertBefore;
            }
            set
            {
                Contract.Requires(value > 0 || value == null);
                _insertBefore = value;
            }
        }

        public long? InsertAfter
        {
            get
            {
                Contract.Ensures(Contract.Result<long?>() > 0 || Contract.Result<long?>() == null);
                return _insertAfter;
            }
            set
            {
                Contract.Requires(value > 0 || value == null);
                _insertAfter = value;
            }
        }
    }
}