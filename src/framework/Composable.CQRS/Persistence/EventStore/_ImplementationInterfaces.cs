using System;
using System.Collections.Generic;
using System.Data.SqlTypes;

namespace Composable.Persistence.EventStore
{
    interface IEventStoreSchemaManager
    {
        void SetupSchemaIfDatabaseUnInitialized();
    }

    interface IEventStoreEventReader
    {
        IReadOnlyList<EventDataRow> GetAggregateHistory(Guid aggregateId, bool takeWriteLock, int startAfterInsertedVersion = 0);
        IEnumerable<EventDataRow> StreamEvents(int batchSize);
        IEnumerable<Guid> StreamAggregateIdsInCreationOrder(Type? eventBaseType = null);
    }

    interface IEventStoreEventWriter
    {
        void Insert(IReadOnlyList<EventDataRow> events);
        void InsertRefactoringEvents(IReadOnlyList<EventDataRow> events);
        void DeleteAggregate(Guid aggregateId);
    }

    interface IAggregateTypeValidator
    {
        void AssertIsValid<TAggregate>();
    }

    interface IEventStorePersistenceLayer
    {
        IEventStoreSchemaManager SchemaManager { get; }
        IEventStoreEventReader EventReader { get; }
        IEventStoreEventWriter EventWriter { get; }
    }

    class EventDataRow
    {
        public EventDataRow(AggregateEvent @event, TypeId eventType, string eventAsJson):this(SqlDecimal.Null, eventType, @event, eventAsJson)
        {}

        EventDataRow(SqlDecimal manualReadOrder, TypeId eventType, AggregateEvent @event, string eventAsJson)
        {
            //urgent: This is sort of horrible. What should this look like? Where should the code be?
            @event.StorageInformation.RefactoringInformation.InsertedVersion = @event.StorageInformation.RefactoringInformation.InsertedVersion > @event.AggregateVersion ? @event.StorageInformation.RefactoringInformation.InsertedVersion : @event.AggregateVersion;

            if(!(manualReadOrder.IsNull || (manualReadOrder.Precision == 38 && manualReadOrder.Scale == 17)))
            {
                throw new ArgumentException($"$$$$$$$$$$$$$$$$$$$$$$$$$ Found decimal with precision: {manualReadOrder.Precision} and scale: {manualReadOrder.Scale}", nameof(manualReadOrder));
            }

            EventJson = eventAsJson;
            EventType = eventType;

            EventId = @event.EventId;
            AggregateVersion = @event.AggregateVersion;
            AggregateId = @event.AggregateId;
            UtcTimeStamp = @event.UtcTimeStamp;
            InsertionOrder = @event.StorageInformation.InsertionOrder;

            RefactoringInformation = @event.StorageInformation.RefactoringInformation;
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

    //Urgent: Everywhere that this type of information occurs, replace with this semantically understandable type instead.
    class AggregateEventRefactoringInformation
    {
       internal int InsertedVersion { get; set; }
        //urgent: See if this cannot be non-nullable.
        internal int? EffectiveVersion { get; set; }
        internal int? ManualVersion { get; set; }

        internal long? Replaces { get; set; }

        internal long? InsertBefore { get; set; }

        internal long? InsertAfter { get; set; }
    }

    class AggregateEventStorageInformation
    {
        internal AggregateEventRefactoringInformation RefactoringInformation { get; set; } = new AggregateEventRefactoringInformation();

        internal long InsertionOrder { get;  set; }
    }
}