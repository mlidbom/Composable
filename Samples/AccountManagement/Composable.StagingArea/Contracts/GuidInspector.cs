using System;

namespace Composable.Contracts
{
    public static class GuidInspector
    {
        public static Inspected<Guid> NotEmpty(this Inspected<Guid> me)
        {
            return me.Inspect(
                inspected => inspected != Guid.Empty,
                badValue => new GuidIsEmptyException(badValue.Name));
        }
    }
}
