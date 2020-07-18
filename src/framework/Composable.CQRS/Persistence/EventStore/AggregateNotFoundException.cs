using System;

namespace Composable.Persistence.EventStore
{
    public class AggregateNotFoundException : Exception
    {
        public AggregateNotFoundException(Guid aggregateId): base($"Aggregate root with Id: {aggregateId} not found")
        {

        }
    }
}