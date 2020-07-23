using System;
using System.Globalization;
using Composable.Contracts;

namespace Composable.SystemCE
{
    static class DateTimeCE
    {
        //Urgent: Review time zone management in all persistence layers, in all code using the persistence layers, and in Serialization. We must have a well thought out approach for ensuring that all of this behaves sanely and consistently. Start by writing tests exploring how the persistence layers and the serializer deals with DateTime instances with all three different kinds when serialized/persisted with one timezone and then read/deserialized in another. How are they converted? Do you get the same Kind back? Does the value change?
        //todo: Do we also need ToLocalTimeSafely?
        ///<summary>Like <see cref="DateTime.ToUniversalTime"/> except it will throw an exception if <see cref="@this"/>.Kind == <see cref="DateTimeKind.Unspecified"/> instead of assuming that Kind == <see cref="DateTimeKind.Local"/> and converting based on that assumption like <see cref="DateTime.ToUniversalTime"/> does.</summary>
        internal static DateTime ToUniversalTimeSafely(this DateTime @this) => @this.AssertHasKind().ToUniversalTime();

        ///<summary>Ensures that a DateTime instance has a Kind specified so that it can be accurately stored, restored, and passed between systems with different time zones without losing information</summary>
        internal static DateTime AssertHasKind(this DateTime @this) => @this.Assert(@this.Kind != DateTimeKind.Unspecified,
                                                                                  @"This DateTime instance does not have a Kind specified. 
This means that it is impossible to accurately persist and restore, or serialize between systems, because it is impossible to know if it refers to the current TimeZone or to UTC timezone. 
Please make sure that all DateTime instances passed to methods which will result in them being persisted or serialized contains a Kind");

        internal static string ToStringInvariant(this DateTime @this) => @this.ToString(CultureInfo.InvariantCulture);
        internal static string ToStringInvariant(this DateTime @this, string format) => @this.ToString(format, CultureInfo.InvariantCulture);

    }
}