using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;

namespace Composable.Persistence.EventStore
{
    interface IAggregateTypeValidator
    {
        void AssertIsValid<TAggregate>();
    }

    interface IEventStorePersistenceLayer
    {
        void SetupSchemaIfDatabaseUnInitialized();


        IReadOnlyList<EventDataRow> GetAggregateHistory(Guid aggregateId, bool takeWriteLock, int startAfterInsertedVersion = 0);
        IEnumerable<EventDataRow> StreamEvents(int batchSize);
        IReadOnlyList<CreationEventRow> ListAggregateIdsInCreationOrder();
        void InsertSingleAggregateEvents(IReadOnlyList<EventDataRow> events);
        void DeleteAggregate(Guid aggregateId);
        void UpdateEffectiveVersions(IReadOnlyList<ManualVersionSpecification> versions);

        class EventNeighborhood
        {
            public SqlDecimal EffectiveReadOrder { get; }
            public SqlDecimal PreviousEventReadOrder { get; }
            public SqlDecimal NextEventReadOrder { get; }

            public EventNeighborhood(SqlDecimal effectiveReadOrder, SqlDecimal previousEventReadOrder, SqlDecimal nextEventReadOrder)
            {
                EffectiveReadOrder = effectiveReadOrder;
                NextEventReadOrder = UseNextIntegerInsteadIfNullSinceThatMeansThisEventIsTheLastInTheEventStore(nextEventReadOrder);
                PreviousEventReadOrder = UseZeroInsteadIfNegativeSinceThisMeansThisIsTheFirstEventInTheEventStore(previousEventReadOrder);
            }

            static SqlDecimal UseZeroInsteadIfNegativeSinceThisMeansThisIsTheFirstEventInTheEventStore(SqlDecimal previousReadOrder) => previousReadOrder > 0 ? previousReadOrder : ToCorrectPrecisionAndScale(new SqlDecimal(0));

            SqlDecimal UseNextIntegerInsteadIfNullSinceThatMeansThisEventIsTheLastInTheEventStore(SqlDecimal nextReadOrder) => !nextReadOrder.IsNull ? nextReadOrder : EffectiveReadOrder + 1;

            static SqlDecimal ToCorrectPrecisionAndScale(SqlDecimal value) => SqlDecimal.ConvertToPrecScale(value, 38, 19);
        }
        IEventStorePersistenceLayer.EventNeighborhood LoadEventNeighborHood(Guid eventId);

        class ManualVersionSpecification
        {
            public ManualVersionSpecification(Guid eventId, int version)
            {
                EventId = eventId;
                EffectiveVersion = version;
            }

            public Guid EventId { get; }
            public int EffectiveVersion { get; }
        }
    }

    class CreationEventRow
    {
        public CreationEventRow(Guid aggregateId, Guid typeId)
        {
            AggregateId = aggregateId;
            TypeId = typeId;
        }
        public Guid AggregateId { get; }
        public Guid TypeId { get; }
    }

    class EventDataRow
    {
        public EventDataRow(IAggregateEvent @event, AggregateEventRefactoringInformation refactoringInformation, Guid eventType, string eventAsJson)
        {
            EventJson = eventAsJson;
            EventType = eventType;

            EventId = @event.EventId;
            AggregateVersion = @event.AggregateVersion;
            AggregateId = @event.AggregateId;
            UtcTimeStamp = @event.UtcTimeStamp;

            RefactoringInformation = refactoringInformation;
        }

        public EventDataRow(EventInsertionSpecification specification, Guid typeId, string eventAsJson)
        {
            var @event = specification.Event;
            EventJson = eventAsJson;
            EventType = typeId;

            EventId = @event.EventId;
            AggregateVersion = @event.AggregateVersion;
            AggregateId = @event.AggregateId;
            UtcTimeStamp = @event.UtcTimeStamp;

            RefactoringInformation = new AggregateEventRefactoringInformation()
                                     {
                                         InsertedVersion = specification.InsertedVersion,
                                         EffectiveVersion = specification.ManualVersion
                                     };
        }

        public EventDataRow(Guid eventType, string eventJson, Guid eventId, int aggregateVersion, Guid aggregateId, DateTime utcTimeStamp, AggregateEventRefactoringInformation refactoringInformation)
        {
            EventType = eventType;
            EventJson = eventJson;
            EventId = eventId;
            AggregateVersion = aggregateVersion;
            AggregateId = aggregateId;
            UtcTimeStamp = utcTimeStamp;

            RefactoringInformation = refactoringInformation;
        }

        public Guid EventType { get; private set; }
        public string EventJson { get; private set; }
        public Guid EventId { get; private set; }
        public int AggregateVersion { get; private set; }

        public Guid AggregateId { get; private set; }
        public DateTime UtcTimeStamp { get; private set; }

        internal AggregateEventRefactoringInformation RefactoringInformation { get; private set; }
    }

    //Urgent: Refactor into enum Replace,InsertBefore,InsertAfter + RefactoredEventId + make all properties non-nullable instead make the whole instance on the event nullable + move data that is on all events elsewhere + split that elsewhere between read and write so that effective order is not nullable when reading and not present when writing. 
    class AggregateEventRefactoringInformation
    {
        internal SqlDecimal? EffectiveOrder { get; set; }
        internal int InsertedVersion { get; set; }

        //urgent: See if this cannot be non-nullable.
        internal int? EffectiveVersion { get; set; }
        internal Guid? Replaces { get; set; }
        internal Guid? InsertBefore { get; set; }
        internal Guid? InsertAfter { get; set; }
    }

    class EventInsertionSpecification
    {
        public EventInsertionSpecification(IAggregateEvent @event) : this(@event, @event.AggregateVersion, null)
        {
        }

        public EventInsertionSpecification(IAggregateEvent @event, int insertedVersion, int? manualVersion)
        {
            Event = @event;
            InsertedVersion = insertedVersion;
            ManualVersion = manualVersion;
        }

        internal IAggregateEvent Event { get; }
        internal int InsertedVersion { get; }
        internal int? ManualVersion { get; }
    }
}