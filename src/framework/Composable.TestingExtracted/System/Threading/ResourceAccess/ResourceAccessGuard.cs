using System;

namespace Composable.Testing.System.Threading.ResourceAccess
{
    public static partial class ResourceAccessGuard
    {
        public static IExclusiveResourceAccessGuard ExclusiveWithTimeout(TimeSpan timeout) => new ExclusiveResourceAccessGuard(timeout);
    }
}
