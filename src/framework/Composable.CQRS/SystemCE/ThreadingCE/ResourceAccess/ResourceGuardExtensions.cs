using System;

namespace Composable.SystemCE.ThreadingCE.ResourceAccess
{
    static class ResourceGuardExtensions
    {

        public static bool TryAwaitCondition(this IResourceGuard @this, TimeSpan timeout, Func<bool> condition)
        {

            try
            {
                using var @lock = @this.AwaitReadLockWhen(timeout, condition);
                return true;
            }
            catch(Exception)
            {
                return false;
            }
        }
    }

    public class AwaitingConditionTimedOutException : Exception
    {
        public AwaitingConditionTimedOutException(AwaitingConditionTimedOutException parent, string message) : base(message, innerException: parent)
        { }

        public AwaitingConditionTimedOutException() : base("Timed out waiting for condition to become true.") {}
    }
}
