using System;

namespace Composable.Persistence.EventStore
{
    class AttemptToSaveEmptyAggregate : Exception
    {
        public AttemptToSaveEmptyAggregate(object value):base($"Attempting to save an: {value.GetType().FullName} that Version=0 and no history to persist.")
        {
        }
    }
}