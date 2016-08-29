using System;

namespace Composable.CQRS.EventSourcing.MicrosoftSQLServer
{
    public class InvalidHistoryException : Exception
    {
        public InvalidHistoryException(Guid aggregateId):base($"AggregateId: {aggregateId}")
        {
        }
    }
}