using System;

namespace Composable.Persistence.Common.EventStore
{
    public class EventDuplicateKeyException : Exception
    {
        //Todo: Oracle exceptions has property: IsRecoverable. Research what this means and if there is something equivalent for the other providers and how this could be useful to us.
        public EventDuplicateKeyException(Exception sqlException) : base(
            @"
A duplicate key exception occurred while persisting new events. 
This is most likely caused by multiple transactions updating the same aggregate and the persistence provider implementation, or database engine, failing to lock appropriately.",
            sqlException) {}
    }
}
