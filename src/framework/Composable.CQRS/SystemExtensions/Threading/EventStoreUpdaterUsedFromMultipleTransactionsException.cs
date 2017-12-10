using System;
using Composable.Persistence.EventStore;

namespace Composable.SystemExtensions.Threading {
    class EventStoreUpdaterUsedFromMultipleTransactionsException : Exception
    {
        public EventStoreUpdaterUsedFromMultipleTransactionsException():base($"Using the {nameof(IEventStoreUpdater)} in multiple transactions is not safe. It makes you vulnerable to hard to debug concurrency issues and is therefore not allowed.") {}
    }
}