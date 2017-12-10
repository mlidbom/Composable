using System;

namespace Composable.Persistence.EventStore
{
    class EventStoreOptimisticConcurrencyException : Exception
    {
        public EventStoreOptimisticConcurrencyException(Exception sqlException) : base(
            $"A primary key violation occurred while persisting new events. This is most likely caused by reusing the same {nameof(IEventStoreUpdater)} instance in more than one transaction.",
            sqlException) {}
    }
}
