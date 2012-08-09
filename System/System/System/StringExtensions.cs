#region usings

using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

#endregion

namespace Composable.System
{
    ///<summary>Contains extensions on <see cref="string"/></summary>
    [Pure]
    public static class StringExtensions
    {
        ///<summary>returns true if me is null, empty or only whitespace</summary>
        public static bool IsNullOrWhiteSpace(this string me)
        {
            return string.IsNullOrWhiteSpace(me);
        }

        ///<summary>Delegates to <see cref="bool.Parse"/></summary>
        public static bool ToBoolean(this string me)
        {
            Contract.Requires(me != null);
            return bool.Parse(me);
        }

        ///<summary>Allows more fluent use of String.Format by exposing it as an extension method.</summary>
        [JetBrains.Annotations.StringFormatMethod("me")]
        public static string FormatWith(this string me, params object[] values)
        {
            Contract.Requires(me != null && values != null);
            return string.Format(me, values);
        }

        /// <summary>Delegates to <see cref="string.Join(string,string[])"/> </summary>
        public static string Join(this IEnumerable<string> strings, string separator)
        {
            Contract.Requires(strings != null);
            Contract.Requires(separator != null);
            return string.Join(separator, strings.ToArray());
        }

        ///<summary>True if the beginning of <paramref name="me"/> is one of the supplied strings.</summary>
        public static bool StartsWith(this string me, params string[] candidates)
        {
            Contract.Requires(candidates != null);
            return candidates.Any(me.StartsWith);
        }
    }
}