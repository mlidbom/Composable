using System;
using System.Collections.Generic;

namespace Composable.Persistence.EventStore.PersistenceLayer
{
    interface IEventStorePersistenceLayer
    {
        void SetupSchemaIfDatabaseUnInitialized();

        IReadOnlyList<EventDataRow> GetAggregateHistory(Guid aggregateId, bool takeWriteLock, int startAfterInsertedVersion = 0);
        IEnumerable<EventDataRow> StreamEvents(int batchSize);
        IReadOnlyList<CreationEventRow> ListAggregateIdsInCreationOrder();
        void InsertSingleAggregateEvents(IReadOnlyList<EventDataRow> events);
        void DeleteAggregate(Guid aggregateId);
        void UpdateEffectiveVersions(IReadOnlyList<VersionSpecification> versions);

        EventNeighborhood LoadEventNeighborHood(Guid eventId);
    }
}
