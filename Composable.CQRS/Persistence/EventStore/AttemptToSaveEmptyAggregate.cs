using System;
using Composable.System;

namespace Composable.CQRS.EventSourcing
{
    class AttemptToSaveEmptyAggregate : Exception
    {
        public AttemptToSaveEmptyAggregate(object value):base("Attempting to save an: {0} that Version=0 and no history to persist.".FormatWith(value.GetType().FullName))
        {
        }
    }
}