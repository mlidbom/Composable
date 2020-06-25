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
        IReadOnlyList<EventReadDataRow> GetAggregateHistory(Guid aggregateId, bool takeWriteLock, int startAfterInsertedVersion = 0);
        IEnumerable<EventReadDataRow> StreamEvents(int batchSize);
        IEnumerable<Guid> StreamAggregateIdsInCreationOrder(Type? eventBaseType = null);
    }

    interface IEventStoreEventWriter
    {
        void Insert(IReadOnlyList<EventWriteDataRow> events);
        void InsertRefactoringEvents(IReadOnlyList<EventWriteDataRow> events);
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

    class EventReadDataRow
    {
        public EventReadDataRow(Guid eventType, string eventJson, Guid eventId, int aggregateVersion, Guid aggregateId, DateTime utcTimeStamp, int insertedVersion, int effectiveVersion, int? manualVersion, long insertionOrder, long? replaces, long? insertBefore, long? insertAfter)
        {
            EventType = eventType;
            EventJson = eventJson;
            EventId = eventId;
            AggregateVersion = aggregateVersion;
            AggregateId = aggregateId;
            UtcTimeStamp = utcTimeStamp;
            InsertedVersion = insertedVersion;
            EffectiveVersion = effectiveVersion;
            ManualVersion = manualVersion;
            InsertionOrder = insertionOrder;
            Replaces = replaces;
            InsertBefore = insertBefore;
            InsertAfter = insertAfter;
        }

        public Guid EventType { get; private set; }
        public string EventJson { get; private set; }
        public Guid EventId { get; private set; }
        public int AggregateVersion { get; private set; }

        public Guid AggregateId { get; private set; }
        public DateTime UtcTimeStamp { get; private set; }

        internal int InsertedVersion { get; private set; }
        internal int EffectiveVersion { get; private set; }
        internal int? ManualVersion { get; private set; }

        internal long InsertionOrder { get; private set; }

        internal long? Replaces { get; private set; }
        internal long? InsertBefore { get; private set; }
        internal long? InsertAfter { get; private set; }
    }

    class EventWriteDataRow
    {
        public EventWriteDataRow(AggregateEvent @event, TypeId eventType, string eventAsJson):this(SqlDecimal.Null, eventType, @event, eventAsJson)
        {}

        EventWriteDataRow(SqlDecimal manualReadOrder, TypeId eventType, AggregateEvent @event, string eventAsJson)
        {
            //urgent: This is sort of horrible. What should this look like? Where should the code be?
            @event.InsertedVersion = @event.InsertedVersion > @event.AggregateVersion ? @event.InsertedVersion : @event.AggregateVersion;

            if(!(manualReadOrder.IsNull || (manualReadOrder.Precision == 38 && manualReadOrder.Scale == 17)))
            {
                throw new ArgumentException($"$$$$$$$$$$$$$$$$$$$$$$$$$ Found decimal with precision: {manualReadOrder.Precision} and scale: {manualReadOrder.Scale}", nameof(manualReadOrder));
            }

            ManualReadOrder = manualReadOrder;
            EventJson = eventAsJson;
            EventType = eventType;

            EventId = @event.EventId;
            AggregateVersion = @event.AggregateVersion;
            AggregateId = @event.AggregateId;
            UtcTimeStamp = @event.UtcTimeStamp;
            InsertedVersion = @event.InsertedVersion;
            ManualVersion = @event.ManualVersion;
            InsertionOrder = @event.InsertionOrder;

            Replaces = @event.Replaces;
            InsertBefore = @event.InsertBefore;
            InsertAfter = @event.InsertAfter;
        }

        public SqlDecimal ManualReadOrder { get; internal set; }

        public TypeId EventType { get; set; }
        public string EventJson { get; private set; }

        public Guid EventId { get; private set; }
        public int AggregateVersion { get; private set; }

        public Guid AggregateId { get; private set; }
        public DateTime UtcTimeStamp { get; private set; }

        internal int InsertedVersion { get; private set; }
        //internal int? EffectiveVersion { get; set; } Only used for when reading.
        internal int? ManualVersion { get; private set; }

        internal long InsertionOrder { get; set; }

        internal long? Replaces { get; private set; }
        internal long? InsertBefore { get; private set; }
        internal long? InsertAfter { get; private set; }

    }

    //Urgent: Everywhere that this type of information occurs, replace with this semantically understandable type instead.
    class EventRefactoringInformation
    {
        internal int InsertedVersion { get; set; }
        internal int EffectiveVersion { get; set; }
        internal int? ManualVersion { get; set; }

        internal long InsertionOrder { get; set; }
        internal long? Replaces { get; set; }

        internal long? InsertBefore { get; set; }

        internal long? InsertAfter { get; set; }
    }

}