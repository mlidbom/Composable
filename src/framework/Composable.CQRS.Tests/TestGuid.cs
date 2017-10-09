using System;

namespace Composable.Tests
{
    static class TestGuid
    {
        public static Guid ToTestGuid(this int @this) => Guid.Parse($"00000000-0000-0000-0000-{@this:000000000000}");
    }
}
