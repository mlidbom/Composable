using System;

namespace Composable.Persistence.EventStore
{
    public class AttemptToSaveEmptyAggregateException : Exception
    {
        public AttemptToSaveEmptyAggregateException(object value):base($"Attempting to save an: {value.GetType().FullName} that Version=0 and no history to persist.")
        {
        }
    }
}