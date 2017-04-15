using System;
using System.Collections.Generic;

namespace Composable.Persistence.EventStore
{
    interface IEventStoreSchemaManager
    {
        IEventTypeToIdMapper IdMapper { get; }
        void SetupSchemaIfDatabaseUnInitialized();
    }

    interface IEventStoreEventReader
    {
        IReadOnlyList<EventDataRow> GetAggregateHistory(Guid aggregateId, bool takeWriteLock, int startAfterInsertedVersion = 0);
        IEnumerable<EventDataRow> StreamEvents(int batchSize);
        IEnumerable<Guid> StreamAggregateIdsInCreationOrder(Type eventBaseType = null);
    }

    interface IEventStoreEventWriter
    {
        void Insert(IEnumerable<AggregateRootEvent> events);
        void InsertRefactoringEvents(IEnumerable<AggregateRootEvent> events);
        void DeleteAggregate(Guid aggregateId);
        void FixManualVersions(Guid aggregateId);
    }


    interface IEventstorePersistenceLayer
    {
        IEventStoreSchemaManager SchemaManager { get; }
        IEventStoreEventReader EventReader { get; }
        IEventStoreEventWriter EventWriter { get; }
    }

    class EventDataRow
    {
        public int EventType { get; set; }
        public string EventJson { get; set; }
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