using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Composable.Contracts;
using JetBrains.Annotations;

namespace Composable.System
{
    ///<summary>Contains extensions on <see cref="string"/></summary>
    static class StringEx
    {
        ///<summary>returns true if me is null, empty or only whitespace</summary>
        [ContractAnnotation("null => true")]
        internal static bool IsNullEmptyOrWhiteSpace(this string? @this) => string.IsNullOrWhiteSpace(@this);

        /// <summary>Delegates to <see cref="string.Join(string,string[])"/> </summary>
        public static string Join(this IEnumerable<string> @this, string separator)
        {
            Contract.ArgumentNotNull(@this, nameof(@this), separator, nameof(separator));

            return string.Join(separator, @this.ToArray());
        }


        internal static string ReplaceInvariant(this string @this, string oldValue, string newValue) => @this.Replace(oldValue, newValue, StringComparison.InvariantCulture);

        internal static bool ContainsInvariant(this string @this, string value) => @this.Contains(value, StringComparison.InvariantCulture);

        internal static int GetHashcodeInvariant(this string @this) => @this.GetHashCode(StringComparison.InvariantCulture);

        public static bool StartsWithInvariant(this string @this, string ending) => @this.StartsWith(ending, StringComparison.InvariantCulture);

        public static bool EndsWithInvariant(this string @this, string ending) => @this.EndsWith(ending, StringComparison.InvariantCulture);

        [StringFormatMethod(formatParameterName:"message")]
        public static string FormatInvariant(string message, params object[] arguments) =>
            string.Format(CultureInfo.InvariantCulture,  message, arguments);
    }
}