using System;

namespace Composable.SystemCE.ThreadingCE.ResourceAccess
{
    static class ResourceGuardExtensions
    {

        public static bool TryAwaitCondition(this IResourceGuard @this, TimeSpan timeout, Func<bool> condition)
        {
            var startTime = DateTime.Now;
            using var @lock = @this.AwaitExclusiveLock(timeout);
            while(!condition())
            {
                if(DateTime.Now - startTime > timeout)
                {
                    return false;
                }
                @lock.TryReleaseAwaitNotificationAndReacquire(timeout);
            }
            return true;
        }



        public static TResult UpdateAndReturn<TResult>(this IResourceGuard @this, Action action, TResult result)
            => @this.Update(() =>
            {
                action();
                return result;
            });
    }

    public class AwaitingConditionTimedOutException : Exception
    {
        public AwaitingConditionTimedOutException(AwaitingConditionTimedOutException parent, string message) : base(message, innerException: parent)
        { }

        public AwaitingConditionTimedOutException() : base("Timed out waiting for condition to become true.") {}
    }
}
