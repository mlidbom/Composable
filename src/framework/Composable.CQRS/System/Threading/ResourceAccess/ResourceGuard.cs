using System;

namespace Composable.System.Threading.ResourceAccess
{
    static partial class ResourceGuard
    {
        public static IResourceGuard WithTimeout(TimeSpan timeout) => new ExclusiveAccessResourceGuard(timeout);
    }
}
