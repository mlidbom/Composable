using System;

namespace Composable.System.Threading.ResourceAccess
{
    static class ResourceAccessGuardExtensions
    {
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
}
