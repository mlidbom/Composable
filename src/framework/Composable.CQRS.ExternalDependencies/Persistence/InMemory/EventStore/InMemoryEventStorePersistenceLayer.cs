using System;
using System.Collections.Generic;
using Composable.Persistence.EventStore;

namespace Composable.Persistence.InMemory.EventStore
{
    class InMemoryEventStorePersistenceLayer : IEventStorePersistenceLayer
    {
        public void SetupSchemaIfDatabaseUnInitialized() { throw new NotImplementedException(); }
        public IReadOnlyList<EventDataRow> GetAggregateHistory(Guid aggregateId, bool takeWriteLock, int startAfterInsertedVersion = 0) => throw new NotImplementedException();
        public IEnumerable<EventDataRow> StreamEvents(int batchSize) => throw new NotImplementedException();
        public IEnumerable<Guid> ListAggregateIdsInCreationOrder(Type? eventBaseType = null) => throw new NotImplementedException();
        public void InsertSingleAggregateEvents(IReadOnlyList<EventDataRow> events) { throw new NotImplementedException(); }
        public void InsertSingleAggregateRefactoringEvents(IReadOnlyList<EventDataRow> events) { throw new NotImplementedException(); }
        public void DeleteAggregate(Guid aggregateId) { throw new NotImplementedException(); }
    }
}
