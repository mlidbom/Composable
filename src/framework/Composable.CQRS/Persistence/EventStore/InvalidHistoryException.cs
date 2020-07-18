using System;

namespace Composable.Persistence.EventStore
{
    public class InvalidHistoryException : Exception
    {
        public InvalidHistoryException(Guid aggregateId):base($"AggregateId: {aggregateId}")
        {
        }
    }
}