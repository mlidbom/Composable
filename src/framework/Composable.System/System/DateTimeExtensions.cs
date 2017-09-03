using System;

namespace Composable.System
{
    static class DateTimeExtensions
    {
        ///<summary>Like <see cref="DateTime.ToUniversalTime"/> except it will throw an exception if <see cref="@this"/>.Kind == <see cref="DateTimeKind.Unspecified"/> instead of assuming that Kind == <see cref="DateTimeKind.Local"/> and converting based on that assumption like <see cref="DateTime.ToUniversalTime"/> does.</summary>
        public static DateTime SafeToUniversalTime(this DateTime @this)
        {
            if(@this.Kind == DateTimeKind.Unspecified)
            {
                throw new Exception();
            }
            return @this.ToUniversalTime();
        }
    }
}