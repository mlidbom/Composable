using System;
using System.Collections.Generic;
using Composable.Persistence.EventStore;

namespace Composable.Persistence.InMemory.EventStore
{
    class InMemoryEventStorePersistenceLayerSchemaManager : IEventStorePersistenceLayer.ISchemaManager
    {
        //Nothing to do for an in-memory storage.
        public void SetupSchemaIfDatabaseUnInitialized() { }
    }

    class InMemoryEventStorePersistenceLayerReader : IEventStorePersistenceLayer.IReader
    {
        public IReadOnlyList<EventDataRow> GetAggregateHistory(Guid aggregateId, bool takeWriteLock, int startAfterInsertedVersion = 0) => throw new NotImplementedException();
        public IEnumerable<EventDataRow> StreamEvents(int batchSize) => throw new NotImplementedException();
        public IEnumerable<Guid> StreamAggregateIdsInCreationOrder(Type? eventBaseType = null) => throw new NotImplementedException();
    }

    class InMemoryEventStorePersistenceLayerWriter : IEventStorePersistenceLayer.IWriter
    {
        public void Insert(IReadOnlyList<EventDataRow> events) { throw new NotImplementedException(); }
        public void InsertRefactoringEvents(IReadOnlyList<EventDataRow> events) { throw new NotImplementedException(); }
        public void DeleteAggregate(Guid aggregateId) { throw new NotImplementedException(); }
    }

    class InMemoryEventStorePersistenceLayer : IEventStorePersistenceLayer
    {
        public InMemoryEventStorePersistenceLayer(IEventStorePersistenceLayer.ISchemaManager schemaManager, IEventStorePersistenceLayer.IReader eventReader, IEventStorePersistenceLayer.IWriter eventWriter)
        {
            SchemaManager = schemaManager;
            EventReader = eventReader;
            EventWriter = eventWriter;
        }

        public IEventStorePersistenceLayer.ISchemaManager SchemaManager { get; }
        public IEventStorePersistenceLayer.IReader EventReader { get; }
        public IEventStorePersistenceLayer.IWriter EventWriter { get; }
    }
}
