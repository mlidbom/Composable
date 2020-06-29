using System;
using System.Collections.Generic;
using Composable.Contracts;

namespace Composable.Persistence.EventStore
{
    interface IAggregateTypeValidator
    {
        void AssertIsValid<TAggregate>();
    }

    interface IEventStorePersistenceLayer
    {
        ISchemaManager SchemaManager { get; }
        IReader EventReader { get; }
        IWriter EventWriter { get; }

        interface ISchemaManager
        {
            void SetupSchemaIfDatabaseUnInitialized();
        }

        interface IReader
        {
            IReadOnlyList<EventDataRow> GetAggregateHistory(Guid aggregateId, bool takeWriteLock, int startAfterInsertedVersion = 0);
            IEnumerable<EventDataRow> StreamEvents(int batchSize);
            IEnumerable<Guid> StreamAggregateIdsInCreationOrder(Type? eventBaseType = null);
        }

        interface IWriter
        {
            void Insert(IReadOnlyList<EventDataRow> events);
            void InsertRefactoringEvents(IReadOnlyList<EventDataRow> events);
            void DeleteAggregate(Guid aggregateId);
        }
    }

    class EventDataRow
    {
        public EventDataRow(IAggregateEvent @event, AggregateEventRefactoringInformation refactoringInformation, TypeId eventType, string eventAsJson)
        {
            EventJson = eventAsJson;
            EventType = eventType;

            EventId = @event.EventId;
            AggregateVersion = @event.AggregateVersion;
            AggregateId = @event.AggregateId;
            UtcTimeStamp = @event.UtcTimeStamp;

            RefactoringInformation = refactoringInformation;
        }

        public EventDataRow(EventInsertionSpecification specification, TypeId typeId, string eventAsJson)
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

        public EventDataRow(TypeId eventType, string eventJson, Guid eventId, int aggregateVersion, Guid aggregateId, DateTime utcTimeStamp, long insertionOrder, AggregateEventRefactoringInformation refactoringInformation)
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

        public TypeId EventType { get; private set; }
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