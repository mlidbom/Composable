using System;
using Composable.Contracts;

namespace Composable.SystemCE
{
    public static class NullableCE
    {
        public static T NotNull<T>(this T? @this) where T : struct => @this ?? throw new ArgumentNullException(nameof(@this));

        public static T NotNull<T>(this T? @this) where T : class => @this ?? throw new ArgumentNullException(nameof(@this));
    }
}
