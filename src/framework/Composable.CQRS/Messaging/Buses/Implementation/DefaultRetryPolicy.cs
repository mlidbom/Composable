using System;

namespace Composable.Messaging.Buses.Implementation
{
    class DefaultRetryPolicy
    {
        static readonly int Tries = 5;
        int _remainingTries;
        // ReSharper disable UnusedParameter.Local parameters are there to enable implementation to take the type of message and exception into account when deciding on whether or not to retry and how long to wait before retrying.
#pragma warning disable IDE0060 // Remove unused parameter
        internal DefaultRetryPolicy(MessageTypes.IMessage message) => _remainingTries = Tries;
        public bool TryAwaitNextRetryTimeForException(Exception exception) => --_remainingTries > 0;
#pragma warning restore IDE0060 // Remove unused parameter
        // ReSharper restore UnusedParameter.Local
    }
}
