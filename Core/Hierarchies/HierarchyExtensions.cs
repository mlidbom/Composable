using System;
using System.Collections.Generic;
using Void.Linq;

namespace Void.Hierarchies
{
    public interface IHierarchy<T>
    {
        Func<T, IEnumerable<T>> GetChildren { get; }
        T Value { get; }
    }

    public static class HierarchyExtensions
    {
        private class Hierarchy<T> : IHierarchy<T>
        {
            public Func<T, IEnumerable<T>> GetChildren { get; private set; }
            public T Value { get; private set; }

            public Hierarchy(T nodeValue, Func<T, IEnumerable<T>> childGetter)
            {
                Value = nodeValue;
                GetChildren = childGetter;
            }
        }

        public static IHierarchy<T> AsHierarchy<T>(this T me, Func<T, IEnumerable<T>> childGetter)
        {
            return new Hierarchy<T>(me, childGetter);
        }

        public static IEnumerable<T> Flatten<T>(this IHierarchy<T> me)
        {
            return Seq.Create(me.Value).FlattenHierarchy(me.GetChildren);
        }
    }
}