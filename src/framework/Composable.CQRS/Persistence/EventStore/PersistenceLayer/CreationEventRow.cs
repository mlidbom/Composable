using System;

namespace Composable.Persistence.EventStore.PersistenceLayer
{
    class CreationEventRow
    {
        public CreationEventRow(Guid aggregateId, Guid typeId)
        {
            AggregateId = aggregateId;
            TypeId = typeId;
        }
        public Guid AggregateId { get; }
        public Guid TypeId { get; }
    }
}
