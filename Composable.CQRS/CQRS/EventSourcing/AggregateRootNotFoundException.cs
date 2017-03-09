using System;

namespace Composable.CQRS.EventSourcing
{
    public class AggregateRootNotFoundException : Exception
    {
        public AggregateRootNotFoundException(Guid aggregateId): base(string.Format("Aggregate root with Id: {0} not found", aggregateId))
        {

        }
    }
}