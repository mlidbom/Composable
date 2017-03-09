#region usings

using System.Collections.Generic;

using System.Linq;
using Composable.Contracts;

#endregion

namespace Composable.System.Linq
{
    /// <summary/>
    public static class SimpleIndexing
    {
        /// <summary>Returns the second element in the IEnumerable</summary>
        public static T Second<T>(this IEnumerable<T> me)
        {
            ContractOptimized.Argument(me, nameof(me))
                             .NotNull();
            return me.ElementAt(1);
        }

        /// <summary>Returns the third element in the IEnumerable</summary>
        public static T Third<T>(this IEnumerable<T> me)
        {
            ContractOptimized.Argument(me, nameof(me))
                             .NotNull();
            return me.ElementAt(2);
        }

        /// <summary>Returns the fourth element in the IEnumerable</summary>
        public static T Fourth<T>(this IEnumerable<T> me)
        {
            ContractOptimized.Argument(me, nameof(me))
                             .NotNull();
            return me.ElementAt(3);
        }

        /// <summary>Returns the fifth element in the IEnumerable</summary>
        public static T Fifth<T>(this IEnumerable<T> me)
        {
            ContractOptimized.Argument(me, nameof(me))
                             .NotNull();
            return me.ElementAt(4);
        }

        /// <summary>Returns the sixth element in the IEnumerable</summary>
        public static T Sixth<T>(this IEnumerable<T> me)
        {
            ContractOptimized.Argument(me, nameof(me))
                             .NotNull();
            return me.ElementAt(5);
        }

        /// <summary>Returns the seventh element in the IEnumerable</summary>
        public static T Seventh<T>(this IEnumerable<T> me)
        {
            ContractOptimized.Argument(me, nameof(me))
                             .NotNull();
            return me.ElementAt(6);
        }

        /// <summary>Returns the eight element in the IEnumerable</summary>
        public static T Eighth<T>(this IEnumerable<T> me)
        {
            ContractOptimized.Argument(me, nameof(me))
                             .NotNull();
            return me.ElementAt(7);
        }

        /// <summary>Returns the ninth element in the IEnumerable</summary>
        public static T Ninth<T>(this IEnumerable<T> me)
        {
            ContractOptimized.Argument(me, nameof(me))
                             .NotNull();
            return me.ElementAt(8);
        }
    }
}