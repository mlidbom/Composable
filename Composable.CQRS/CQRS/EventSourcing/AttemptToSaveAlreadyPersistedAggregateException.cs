using System;

namespace Composable.CQRS.EventSourcing
{
    public class AttemptToSaveAlreadyPersistedAggregateException : Exception
    {
        public AttemptToSaveAlreadyPersistedAggregateException(IEventStored aggregate)
            :base(
                string.Format("Instance of {0} with Id: {1} has already been persisted. To update it, load it from a session and modify it rather than attempting to call save",
                              aggregate.GetType().FullName, aggregate.Id))
        {

        }
    }
}