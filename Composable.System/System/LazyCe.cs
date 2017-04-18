using System;

namespace Composable.System
{
    internal static class LazyCe
    {
        internal static void TouchValue<T>(this Lazy<T> @this)
        {
            // ReSharper disable once UnusedVariable The whole point of the method is to be able to fetch the value without being forced to do something with it...
            var value = @this.Value;
        }
    }
}
