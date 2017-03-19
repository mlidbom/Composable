using System;

namespace Composable.CQRS.EventSourcing.MicrosoftSQLServer
{
    class InvalidHistoryException : Exception
    {
        public InvalidHistoryException(Guid aggregateId):base($"AggregateId: {aggregateId}")
        {
        }
    }
}