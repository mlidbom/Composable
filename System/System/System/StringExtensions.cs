using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text;
using Composable.System.Linq;
using System.Linq;

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
        public static string FormatWith(this string me, params object[] values)
        {
            Contract.Requires(me != null && values != null);
            return string.Format(me, values);
        }

        /// <summary>Delegates to <see cref="string.Join(string,string[])"/> </summary>
        public static string Join(this IEnumerable<string> strings, string separator)
        {
            Contract.Requires(strings!=null);
            Contract.Requires(separator != null);
            return string.Join(separator, strings.ToArray());
        }
    }
}