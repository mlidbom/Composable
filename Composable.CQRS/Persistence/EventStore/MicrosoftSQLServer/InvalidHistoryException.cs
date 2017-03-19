using System;

namespace Composable.Persistence.EventStore.MicrosoftSQLServer
{
    class InvalidHistoryException : Exception
    {
        public InvalidHistoryException(Guid aggregateId):base($"AggregateId: {aggregateId}")
        {
        }
    }
}