using System;

namespace Composable.System.Threading.ResourceAccess
{
    static partial class GuardedResource
    {
        public static IGuardedResource WithTimeout(TimeSpan timeout) => new ExclusiveAccessGuardedResource(timeout);
    }
}
