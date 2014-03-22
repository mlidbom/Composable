using System;
using Composable.Contracts.Tests;

namespace Composable.Contracts
{
    public static class ObjectNullInspector
    {
        public static Inspected<Guid> NotEmpty(this Inspected<Guid> me)
        {
            return me.Inspect(
                inspected => inspected != Guid.Empty,
                badValue => new GuidIsEmptyException(badValue.Name));
        }
    }

    public static class GuidInspector
    {
        public static Inspected<TArgument> NotNull<TArgument>(this Inspected<TArgument> me)
            where TArgument : class
        {
            return me.Inspect(
                inspected => inspected != null,
                badValue => new ObjectIsNullException(badValue.Name));
        }
    }
}
