using System;

namespace Composable.CQRS.EventSourcing
{
    class AggregateRootNotFoundException : Exception
    {
        public AggregateRootNotFoundException(Guid aggregateId): base($"Aggregate root with Id: {aggregateId} not found")
        {

        }
    }
}