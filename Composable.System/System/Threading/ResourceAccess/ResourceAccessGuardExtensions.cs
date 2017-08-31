using System;

namespace Composable.System.Threading.ResourceAccess
{
    static class ResourceAccessGuardExtensions
    {

        public static IExclusiveResourceLock AwaitExclusiveLockWhen(this IExclusiveResourceAccessGuard @this, Func<bool> condition)
         => @this.AwaitExclusiveLockWhen(@this.DefaultTimeout, condition);


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

        public static IExclusiveResourceLock AwaitExclusiveLockWhen(this IExclusiveResourceAccessGuard @this, TimeSpan timeout, Func<bool> condition)
        {
            IExclusiveResourceLock exclusiveLock = null;
            try
            {
                exclusiveLock = @this.AwaitExclusiveLock();
                while (!condition())
                {
                    exclusiveLock.ReleaseLockAwaitUpdateNotificationAndAwaitExclusiveLock(timeout);
                }
                return exclusiveLock;
            }
            catch (Exception)
            {
                exclusiveLock?.Dispose();
                throw;
            }
        }
    }
}
