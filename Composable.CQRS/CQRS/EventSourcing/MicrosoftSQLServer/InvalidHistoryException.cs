using System;

namespace Composable.CQRS.CQRS.EventSourcing.MicrosoftSQLServer
{
    class InvalidHistoryException : Exception
    {
        public InvalidHistoryException(Guid aggregateId):base($"AggregateId: {aggregateId}")
        {
        }
    }
}