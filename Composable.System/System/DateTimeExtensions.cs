using System;
using System.Diagnostics.Contracts;

namespace Composable.System
{
    public static class DateTimeExtensions
    {
        ///<summary>Like <see cref="DateTime.ToUniversalTime"/> except it will throw an exception if <see cref="@this"/>.Kind == <see cref="DateTimeKind.Unspecified"/> instead of assuming that Kind == <see cref="DateTimeKind.Local"/> and converting based on that assumption like <see cref="DateTime.ToUniversalTime"/> does.</summary>
        public static DateTime SafeToUniversalTime(this DateTime @this)
        {
            @this.AssertHasDeterministicValue();
            return @this.ToUniversalTime();
        }

        ///<summary>Throws an exception if @this.Kind == <see cref="DateTimeKind.Unspecified"/>.</summary>
        public static void AssertHasDeterministicValue(this DateTime @this)
        {
            Contract.Requires(@this.HasDeterministicValue());
        }

        [Pure]
        ///<summary>True if @this.Kind != DateTimeKind.Unspecified. If Kind is Unspecified it is not really possible to know which time this represents. It is not possible to schedule an event to take place at this time etc. In order to be able to do so you must know the timezone for a datetime which you only know if it is not Unspecified. </summary>
        public static bool HasDeterministicValue(this DateTime @this) { return @this.Kind != DateTimeKind.Unspecified; }
    }
}