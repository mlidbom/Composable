using System;
using System.Collections.Generic;

using System.Linq;
using Composable.Contracts;
using Composable.GenericAbstractions.Wrappers;
using Composable.System.Linq;

namespace Composable.GenericAbstractions.Hierarchies
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
    static class HierarchyExtensions
    {
        class Hierarchy<T> : IAutoHierarchy<T>
        {
            readonly Func<T, IEnumerable<T>> _childGetter;

            public IEnumerable<IAutoHierarchy<T>> Children { get { return _childGetter(Wrapped).Select(child => child.AsHierarchy(_childGetter)); } }

            public T Wrapped { get; private set; }

            public Hierarchy(T nodeValue, Func<T, IEnumerable<T>> childGetter)
            {
                ContractOptimized.Argument(childGetter, nameof(childGetter)).NotNull();
                Wrapped = nodeValue;
                _childGetter = childGetter;
            }
        }

        /// <summary>
        /// Returns an <see cref="IAutoHierarchy{T}"/> where <see cref="IWrapper{T}.Wrapped"/> is <paramref name="me"/> and
        /// <see cref="IHierarchy{T}.Children"/> is implemented via delegation to <paramref name="childGetter"/>
        /// </summary>
        public static IAutoHierarchy<T> AsHierarchy<T>(this T me, Func<T, IEnumerable<T>> childGetter)
        {
            ContractOptimized.Argument(me, nameof(me), childGetter, nameof(childGetter)).NotNull();
            return Contract.Return(new Hierarchy<T>(me, childGetter), inspect => inspect.NotNull());
        }

        /// <summary>
        /// Returns <paramref name="root"/> and all the objects in the hierarchy
        /// below <paramref name="root"/> flattened into a sequence
        /// </summary>
        public static IEnumerable<T> Flatten<T>(this T root) where T : IHierarchy<T>
        {
            ContractOptimized.Argument(root, nameof(root)).NotNull();
            return Seq.Create(root).FlattenHierarchy(me => me.Children);
        }


        /// <summary>
        /// Given a sequence of <see cref="IAutoHierarchy{T}"/> returns a sequence containing the wrapped T values.
        /// </summary>
        public static IEnumerable<T> Unwrap<T>(this IEnumerable<IAutoHierarchy<T>> root)
        {
            ContractOptimized.Argument(root, nameof(root)).NotNull();
            return root.Select(me => me.Wrapped);
        }
    }
}