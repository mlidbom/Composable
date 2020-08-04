using System;

namespace Composable.SystemCE.ThreadingCE.ResourceAccess
{
    static partial class ResourceGuard
    {
        static readonly TimeSpan InfiniteTimeout = -1.Milliseconds();

        public static IResourceGuard WithTimeout(TimeSpan timeout) => new ExclusiveAccessResourceGuard(timeout);
        public static IResourceGuard Create() => new ExclusiveAccessResourceGuard(InfiniteTimeout);
    }
}
