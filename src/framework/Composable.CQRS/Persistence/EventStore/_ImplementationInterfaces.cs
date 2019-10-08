﻿using System;
using System.Collections.Generic;
using System.Data.SqlTypes;

namespace Composable.Persistence.EventStore
{
    interface IEventStoreSchemaManager
    {
        IEventTypeToIdMapper IdMapper { get; }
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
        public EventReadDataRow(int eventType, string eventJson, Guid eventId, int aggregateVersion, Guid aggregateId, DateTime utcTimeStamp, int insertedVersion, int? effectiveVersion, int? manualVersion, long insertionOrder, long? replaces, long? insertBefore, long? insertAfter)
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

        public int EventType { get; set; }
        public string EventJson { get; set; }
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
    }

    class EventWriteDataRow
    {
        public EventWriteDataRow(AggregateEvent @event, string eventAsJson):this(SqlDecimal.Null, @event, eventAsJson)
        {}

        public EventWriteDataRow(EventWriteDataRow source, SqlDecimal manualReadOrder) : this(manualReadOrder, source.Event, source.EventJson)
        { }

        EventWriteDataRow(SqlDecimal manualReadOrder, AggregateEvent @event, string eventAsJson)
        {
            if(!(manualReadOrder.IsNull || (manualReadOrder.Precision == 38 && manualReadOrder.Scale == 17)))
            {
                throw new ArgumentException($"$$$$$$$$$$$$$$$$$$$$$$$$$ Found decimal with precision: {manualReadOrder.Precision} and scale: {manualReadOrder.Scale}", nameof(manualReadOrder));
            }

            Event = @event;
            ManualReadOrder = manualReadOrder;
            EventJson = eventAsJson;

            EventId = @event.EventId;
            AggregateVersion = @event.AggregateVersion;
            AggregateId = @event.AggregateId;
            UtcTimeStamp = @event.UtcTimeStamp;
            InsertedVersion = @event.InsertedVersion;
            EffectiveVersion = @event.EffectiveVersion;
            ManualVersion = @event.ManualVersion;
            InsertionOrder = @event.InsertionOrder;

            Replaces = @event.Replaces;
            InsertBefore = @event.InsertBefore;
            InsertAfter = @event.InsertAfter;
        }

        public SqlDecimal ManualReadOrder { get; private set; }
        public AggregateEvent Event { get; private set; }


        public int EventType { get; set; }
        public string EventJson { get; set; }

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

    }

}