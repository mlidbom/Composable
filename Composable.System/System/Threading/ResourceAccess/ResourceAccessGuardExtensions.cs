using System;

namespace Composable.System.Threading.ResourceAccess
{
    static class ResourceAccessGuardExtensions
    {
        public static IExclusiveResourceLock AwaitExclusiveLockWhen(this IExclusiveResourceAccessGuard @this, TimeSpan timeout, Func<bool> condition)
        {
            IExclusiveResourceLock exclusiveLock = null;
            try
            {
                exclusiveLock = @this.AwaitExclusiveLock();
                while(!condition())
                {
                    exclusiveLock.ReleaseLockAwaitUpdateNotificationAndAwaitExclusiveLock(timeout);
                }
                return exclusiveLock;
            }
            catch (AwaitingUpdateNotificationTimedOutException notificationTimeout)
            {
                exclusiveLock?.Dispose();
                throw new AwaitingConditionTimedOutException(notificationTimeout);
            }
            catch (Exception)
            {
                exclusiveLock?.Dispose();
                throw;
            }
        }

        public static bool TryAwait(this IExclusiveResourceAccessGuard @this, TimeSpan timeout, Func<bool> condition)
        {
            var startTime = DateTime.Now;
            using (var @lock = @this.AwaitExclusiveLock(timeout))
            {
                while (!condition())
                {
                    if (DateTime.Now - startTime > timeout)
                    {
                        return false;
                    }
                    @lock.TryReleaseLockAwaitUpdateNotificationAndAwaitExclusiveLock(timeout);
                }
                return true;
            }
        }

        public static void ExecuteWithExclusiveLock(this IExclusiveResourceAccessGuard @lock, Action action)
        {
            using(@lock.AwaitExclusiveLock())
            {
                action();
            }
        }

        public static TResult ExecuteWithExclusiveLock<TResult>(this IExclusiveResourceAccessGuard @lock, Func<TResult> function)
        {
            using(@lock.AwaitExclusiveLock())
            {
                return function();
            }
        }

        public static void ExecuteWithExclusiveLock(this IExclusiveResourceAccessGuard @lock, TimeSpan timeout, Action action)
        {
            using(@lock.AwaitExclusiveLock())
            {
                action();
            }
        }

        public static TResult ExecuteWithExclusiveLock<TResult>(this IExclusiveResourceAccessGuard @lock, TimeSpan timeout, Func<TResult> function)
        {
            using(@lock.AwaitExclusiveLock())
            {
                return function();
            }
        }
    }

    class AwaitingConditionTimedOutException : Exception
    {
        public AwaitingConditionTimedOutException(AwaitingUpdateNotificationTimedOutException notificationTimeout) : base("Timed out waiting for condiditon to become true. Never got any update notifications", innerException: notificationTimeout){  }
    }
}
