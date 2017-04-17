using System;

namespace Composable.Persistence.EventStore
{
    class AggregateRootNotFoundException : Exception
    {
        public AggregateRootNotFoundException(Guid aggregateId): base($"Aggregate root with Id: {aggregateId} not found")
        {

        }
    }
}