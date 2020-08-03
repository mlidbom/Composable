using System;

namespace Composable.SystemCE.ThreadingCE.ResourceAccess
{
    public class AwaitingConditionTimedOutException : Exception
    {
        public AwaitingConditionTimedOutException(AwaitingConditionTimedOutException parent, string message) : base(message, innerException: parent)
        { }

        public AwaitingConditionTimedOutException() : base("Timed out waiting for condition to become true.") {}
    }
}
