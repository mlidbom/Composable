using System;

namespace Composable.Persistence.EventStore
{
    class AggregateNotFoundException : Exception
    {
        public AggregateNotFoundException(Guid aggregateId): base($"Aggregate root with Id: {aggregateId} not found")
        {

        }
    }
}