using System;
using System.Collections.Generic;
using System.Linq;

namespace Void.Linq
{
    public static class LinqExtensions
    {
        public static IEnumerable<T> Append<T>(this IEnumerable<T> source, params T[] instances)
        {
            return source.Concat(instances);
        }


        //todo: Figure out why this method does not resolve as an extension method.
        //is the type inference in C# to weak?
        /// <summary>
        /// Acting on an <see cref="IEnumerable{T}"/> <paramref name="me"/> where T is an <see cref="IEnumerable{TChild}"/>
        /// returns an <see cref="IEnumerable{TChild}"/> aggregating all the TChild instances
        /// 
        /// Using SelectMany(x=>x) is ugly and unintuitive.
        /// This method provides an intuitively named alternative.
        /// </summary>
        /// <typeparam name="T">A type implementing <see cref="IEnumerable{TChild}"/></typeparam>
        /// <typeparam name="TChild">The type contained in the nested enumerables.</typeparam>
        /// <param name="me">the collection to act upon</param>
        /// <returns>All the objects in all the nested collections </returns>
        public static IEnumerable<TChild> Flatten<T, TChild>(IEnumerable<T> me) where T : IEnumerable<TChild>
        {
            return me.SelectMany(obj => obj);
        }
    }
}