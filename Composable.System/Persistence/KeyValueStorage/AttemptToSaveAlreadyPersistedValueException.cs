using System;

namespace Composable.Persistence.KeyValueStorage
{
    class AttemptToSaveAlreadyPersistedValueException : Exception
    {
        public AttemptToSaveAlreadyPersistedValueException(object key, object value)
            : base(
                   $"Instance of {value.GetType() .FullName} with Id: {key} has already been persisted. To update it, load it from a session and modify it rather than attempting to call save")
        {

        }
    }
}