using System;

namespace Composable.System.Threading.ResourceAccess
{
    static partial class ResourceGuard
    {
        public static IResourceGuard WithTimeout(TimeSpan timeout) => new ExclusiveAccessResourceGuard(timeout);

        public static void WithExclusiveLock(object @lock, Action action)
        {
            lock(@lock)
            {
                action();
            }
        }

        public static TResult WithExclusiveLock<TResult>(object @lock, Func<TResult> func)
        {
            lock (@lock)
            {
                return func();
            }
        }
    }
}
