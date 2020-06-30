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
        IEnumerable<Guid> ListAggregateIdsInCreationOrder(Type? eventBaseType = null);
        void InsertSingleAggregateEvents(IReadOnlyList<EventDataRow> events);
        void DeleteAggregate(Guid aggregateId);
        void FixManualVersions(Guid aggregateId);

        class EventNeighborhood
        {
            long InsertionOrder { get; }
            public SqlDecimal EffectiveReadOrder { get; }
            public SqlDecimal PreviousEventReadOrder { get; }
            public SqlDecimal NextEventReadOrder { get; }

            public EventNeighborhood(long insertionOrder, SqlDecimal effectiveReadOrder, SqlDecimal previousEventReadOrder, SqlDecimal nextEventReadOrder)
            {
                InsertionOrder = insertionOrder;
                EffectiveReadOrder = effectiveReadOrder;
                NextEventReadOrder = UseNextIntegerInsteadIfNullSinceThatMeansThisEventIsTheLastInTheEventStore(nextEventReadOrder);
                PreviousEventReadOrder = UseZeroInsteadIfNegativeSinceThisMeansThisIsTheFirstEventInTheEventStore(previousEventReadOrder);
            }

            static SqlDecimal UseZeroInsteadIfNegativeSinceThisMeansThisIsTheFirstEventInTheEventStore(SqlDecimal previousReadOrder) => previousReadOrder > 0 ? previousReadOrder : ToCorrectPrecisionAndScale(new SqlDecimal(0));

            SqlDecimal UseNextIntegerInsteadIfNullSinceThatMeansThisEventIsTheLastInTheEventStore(SqlDecimal nextReadOrder) => !nextReadOrder.IsNull ? nextReadOrder : ToCorrectPrecisionAndScale(new SqlDecimal(InsertionOrder + 1));

            static SqlDecimal ToCorrectPrecisionAndScale(SqlDecimal value) => SqlDecimal.ConvertToPrecScale(value, 38, 19);
        }
        IEventStorePersistenceLayer.EventNeighborhood LoadEventNeighborHood(Guid eventId);
        void SaveRefactoringEventsWithinReadOrderRange(EventDataRow[] newEvents, SqlDecimal rangeStart, SqlDecimal rangeEnd);
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
                                         ManualVersion = specification.ManualVersion
                                     };
        }

        public EventDataRow(Guid eventType, string eventJson, Guid eventId, int aggregateVersion, Guid aggregateId, DateTime utcTimeStamp, long insertionOrder, AggregateEventRefactoringInformation refactoringInformation)
        {
            EventType = eventType;
            EventJson = eventJson;
            EventId = eventId;
            AggregateVersion = aggregateVersion;
            AggregateId = aggregateId;
            UtcTimeStamp = utcTimeStamp;
            InsertionOrder = insertionOrder;

            RefactoringInformation = refactoringInformation;
        }

        public Guid EventType { get; private set; }
        public string EventJson { get; private set; }
        public Guid EventId { get; private set; }
        public int AggregateVersion { get; private set; }

        public Guid AggregateId { get; private set; }
        public DateTime UtcTimeStamp { get; private set; }

        //urgent: not happy about this having public setter.
        internal long InsertionOrder { get; set; }

        internal AggregateEventRefactoringInformation RefactoringInformation { get; private set; }
    }

    class AggregateEventRefactoringInformation
    {
        internal int InsertedVersion { get; set; }

        //urgent: See if this cannot be non-nullable.
        internal int? EffectiveVersion { get; set; }
        internal int? ManualVersion { get; set; }
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