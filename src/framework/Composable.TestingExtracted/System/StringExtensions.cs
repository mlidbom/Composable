using System.Collections.Generic;
using System.Linq;
using Composable.Contracts;
using JetBrains.Annotations;

namespace Composable.System
{
    ///<summary>Contains extensions on <see cref="string"/></summary>
    static class StringExtensions
    {
        ///<summary>returns true if me is null, empty or only whitespace</summary>
        [ContractAnnotation("null => true")]
        internal static bool IsNullOrWhiteSpace(this string me) => string.IsNullOrWhiteSpace(me);

        /// <summary>Delegates to <see cref="string.Join(string,string[])"/> </summary>
        public static string Join(this IEnumerable<string> strings, string separator)
        {
            ContractOptimized.Argument(strings, nameof(strings), separator, nameof(separator))
                             .NotNull();

            return string.Join(separator, strings.ToArray());
        }
    }
}