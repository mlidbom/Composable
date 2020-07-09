using System;

namespace Composable.Persistence.Common.EventStore
{
    class EventDuplicateKeyException : Exception
    {
        public EventDuplicateKeyException(Exception sqlException) : base(
            $@"
A duplicate key exception occurred while persisting new events. 
This is most likely caused by multiple transactions updating the same aggregate and the persistence provider implementation, or database engine, failing to lock appropriately.",
            sqlException) {}
    }
}
