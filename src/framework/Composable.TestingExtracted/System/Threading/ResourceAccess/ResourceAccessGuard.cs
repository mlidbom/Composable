using System;

namespace Composable.Testing.System.Threading.ResourceAccess
{
    static partial class ResourceAccessGuard
    {
        public static IExclusiveResourceAccessGuard ExclusiveWithTimeout(TimeSpan timeout) => new ExclusiveResourceAccessGuard(timeout);
        public static ISharedResourceAccessGuard CreateWithMaxSharedLocksAndTimeout(int maxSharedLocks, TimeSpan defaultTimeout) => new SharedResourceAccessGuard(maxSharedLocks, defaultTimeout);
    }
}
