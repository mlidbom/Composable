using System;

namespace Composable.Persistence.KeyValueStorage
{
    class AttemptToSaveAlreadyPersistedValueException : Exception
    {
        public AttemptToSaveAlreadyPersistedValueException(object key, object value)
            : base(
                string.Format("Instance of {0} with Id: {1} has already been persisted. To update it, load it from a session and modify it rather than attempting to call save",
                              value.GetType().FullName, key))
        {

        }
    }
}