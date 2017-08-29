using System;

namespace Composable.System.Threading.ResourceAccess
{
    static class ResourceLockManagerExtentions
    {
        public static void ExecuteWithExclusiveLock(this IExclusiveResourceLockManager @lock, Action action)
        {
            using(@lock.AwaitExclusiveLock())
            {
                action();
            }
        }

        public static TResult ExecuteWithExclusiveLock<TResult>(this IExclusiveResourceLockManager @lock, Func<TResult> function)
        {
            using(@lock.AwaitExclusiveLock())
            {
                return function();
            }
        }

        public static void ExecuteWithExclusiveLock(this IExclusiveResourceLockManager @lock, TimeSpan timeout, Action action)
        {
            using(@lock.AwaitExclusiveLock())
            {
                action();
            }
        }

        public static TResult ExecuteWithExclusiveLock<TResult>(this IExclusiveResourceLockManager @lock, TimeSpan timeout, Func<TResult> function)
        {
            using(@lock.AwaitExclusiveLock())
            {
                return function();
            }
        }
    }
}
