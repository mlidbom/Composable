using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Void.Linq;
using System.Linq;
using Void.Wrappers;

namespace Void.Hierarchies
{
    /// <summary>
    /// Represents a hierarchy in which the instances in the hierarchy do not themselves 
    /// implement <see cref="IHierarchy{T}"/>.
    /// 
    /// <example>
    /// For instance you could use <see cref="HierarchyExtensions.AsHierarchy{T}"/> like this:
    /// <code>
    ///     directoryName.AsHierarchy&lt;
    /// </code>
    /// </example>
    /// 
    /// </summary>
    public interface IAutoHierarchy<T> : IHierarchy<IAutoHierarchy<T>>, IWrapper<T>
    {
    }

    /// <summary>
    /// Provides extension methods for working with hierarchical data.
    /// </summary>
    public static class HierarchyExtensions
    {
        private class Hierarchy<T> : IAutoHierarchy<T>
        {
            private readonly Func<T, IEnumerable<T>> childGetter;

            [ContractInvariantMethod]
            private void ObjectInvariant()
            {
                Contract.Invariant(childGetter != null);
            }

            public IEnumerable<IAutoHierarchy<T>> Children
            {
                get
                {
                    return childGetter(Wrapped).Select(child => child.AsHierarchy(childGetter));
                }
            }

            public T Wrapped { get; private set; }

            public Hierarchy(T nodeValue, Func<T, IEnumerable<T>> childGetter)
            {
                Wrapped = nodeValue;
                this.childGetter = childGetter;
            }
        }

        /// <summary>
        /// Returns an <see cref="IAutoHierarchy{T}"/> where <see cref="IWrapper{T}.Wrapped"/> is <paramref name="me"/> and
        /// <see cref="IHierarchy{T}.Children"/> is implemented via delegation to <paramref name="childGetter"/>
        /// </summary>
        public static IAutoHierarchy<T> AsHierarchy<T>(this T me, Func<T, IEnumerable<T>> childGetter)
        {
            Contract.Requires(me != null && childGetter != null);
            Contract.Ensures(Contract.Result<IAutoHierarchy<T>>() != null);
            return new Hierarchy<T>(me, childGetter);
        }

        /// <summary>
        /// Returns <paramref name="root"/> and all the objects in the hierarchy
        /// below <paramref name="root"/> flattened into a sequence
        /// </summary>
        public static IEnumerable<T> Flatten<T>(this T root) where T : IHierarchy<T>
        {
            Contract.Requires(root != null);
            Contract.Ensures(Contract.Result<IEnumerable<T>>() != null);
            return Seq.Create(root).FlattenHierarchy(me => me.Children);
        }


        /// <summary>
        /// Given a sequence of <see cref="IAutoHierarchy{T}"/> returns a sequence containing the wrapped T values.
        /// </summary>
        public static IEnumerable<T> Unwrap<T>(this IEnumerable<IAutoHierarchy<T>> root) 
        {
            Contract.Requires(root != null);
            Contract.Ensures(Contract.Result<IEnumerable<T>>() != null);
            return root.Select(me => me.Wrapped);
        }
    }
}