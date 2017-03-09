#region usings

using System.Collections.Generic;

using System.Linq;
using Composable.Contracts;

#endregion

namespace Composable.System
{
    ///<summary>Contains extensions on <see cref="string"/></summary>
    public static class StringExtensions
    {
        ///<summary>returns true if me is null, empty or only whitespace</summary>
        public static bool IsNullOrWhiteSpace(this string me)
        {
            return string.IsNullOrWhiteSpace(me);
        }

        ///<summary>Allows more fluent use of String.Format by exposing it as an extension method.</summary>
        [JetBrains.Annotations.StringFormatMethod("me")]
        public static string FormatWith(this string me, params object[] values)
        {
            ContractOptimized.Argument(me, nameof(me), values, nameof(values))
                             .NotNull();

            return string.Format(me, values);
        }

        /// <summary>Delegates to <see cref="string.Join(string,string[])"/> </summary>
        public static string Join(this IEnumerable<string> strings, string separator)
        {
            ContractOptimized.Argument(strings, nameof(strings), separator, nameof(separator))
                             .NotNull();

            return string.Join(separator, strings.ToArray());
        }
    }
}