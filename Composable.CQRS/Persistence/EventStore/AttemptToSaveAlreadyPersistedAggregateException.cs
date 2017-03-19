using System;

namespace Composable.CQRS.EventSourcing
{
    class AttemptToSaveAlreadyPersistedAggregateException : Exception
    {
        public AttemptToSaveAlreadyPersistedAggregateException(IEventStored aggregate)
            :base(
                  $"Instance of {aggregate.GetType() .FullName} with Id: {aggregate.Id} has already been persisted. To update it, load it from a session and modify it rather than attempting to call save")
        {

        }
    }
}