using System;
using Composable.Persistence.EventStore;

namespace Composable.Persistence.SqlServer.EventStore
{
    class SqlServerEventStoreOptimisticConcurrencyException : Exception
    {
        public SqlServerEventStoreOptimisticConcurrencyException(Exception sqlException) : base(
            $"A primary key violation occurred while persisting new events. This is most likely caused by reusing the same {nameof(IEventStoreUpdater)} instance in more than one transaction.",
            sqlException) {}
    }
}
