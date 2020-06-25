using System;
using System.Collections.Generic;
using Composable.Persistence.EventStore;

namespace Composable.Persistence.InMemory.EventStore
{
    class InMemoryEventStoreSchemaManager : IEventStoreSchemaManager
    {
        //Nothing to do for an in-memory storage.
        public void SetupSchemaIfDatabaseUnInitialized() { }
    }

    class InMemoryEventStoreEventReader : IEventStoreEventReader
    {
        public IReadOnlyList<EventReadDataRow> GetAggregateHistory(Guid aggregateId, bool takeWriteLock, int startAfterInsertedVersion = 0) => throw new NotImplementedException();
        public IEnumerable<EventReadDataRow> StreamEvents(int batchSize) => throw new NotImplementedException();
        public IEnumerable<Guid> StreamAggregateIdsInCreationOrder(Type? eventBaseType = null) => throw new NotImplementedException();
    }

    class InMemoryEventStoreEventWriter : IEventStoreEventWriter
    {
        public void Insert(IReadOnlyList<EventWriteDataRow> events) { throw new NotImplementedException(); }
        public void InsertRefactoringEvents(IReadOnlyList<EventWriteDataRow> events) { throw new NotImplementedException(); }
        public void DeleteAggregate(Guid aggregateId) { throw new NotImplementedException(); }
    }

    class InMemoryEventStorePersistenceLayer : IEventStorePersistenceLayer
    {
        public InMemoryEventStorePersistenceLayer(IEventStoreSchemaManager schemaManager, IEventStoreEventReader eventReader, IEventStoreEventWriter eventWriter)
        {
            SchemaManager = schemaManager;
            EventReader = eventReader;
            EventWriter = eventWriter;
        }

        public IEventStoreSchemaManager SchemaManager { get; }
        public IEventStoreEventReader EventReader { get; }
        public IEventStoreEventWriter EventWriter { get; }
    }
}
