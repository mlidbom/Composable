using System.Collections.Generic;
using System.Linq;

namespace Void.Linq
{
    public static class SimpleIndexing
    {
        public static T AtIndex<T>(this IEnumerable<T> me, int index)
        {
            return me.Skip(index).First();
        }

        public static T Second<T>(this IEnumerable<T> me)
        {
            return me.AtIndex(1);
        }

        public static T Third<T>(this IEnumerable<T> me)
        {
            return me.AtIndex(2);
        }

        public static T Fourth<T>(this IEnumerable<T> me)
        {
            return me.AtIndex(3);
        }

        public static T Fifth<T>(this IEnumerable<T> me)
        {
            return me.AtIndex(4);
        }

        public static T Sixth<T>(this IEnumerable<T> me)
        {
            return me.AtIndex(5);
        }

        public static T Seventh<T>(this IEnumerable<T> me)
        {
            return me.AtIndex(6);
        }

        public static T Eighth<T>(this IEnumerable<T> me)
        {
            return me.AtIndex(7);
        }

        public static T Ninth<T>(this IEnumerable<T> me)
        {
            return me.AtIndex(8);
        }
    }
}