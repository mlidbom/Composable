using System;
using System.Collections.Generic;
using Void.Linq;
using System.Linq;

namespace Void.Hierarchies
{
    /// <summary>
    /// Provides a minimal interface for representing a data type which 
    /// is hierarchical in the form that each instance has a collection 
    /// of other instances.
    /// 
    /// Implementing this interface gives access to all the extension methods 
    /// implemented upon it which is the main purpose of doing so.
    /// 
    /// Normally it would be recommended to use exlicit interface implementation
    /// in order not to pollute the implementing class' public interface.
    /// 
    /// <example>
    /// A simplistic example might look like this:
    /// <code>
    ///    class Person : IHierarchy<Person>
    ///    {
    ///        public Person()
    ///        {
    ///            Children = new List<Person>();
    ///        }
    /// 
    ///        IEnumerable<IHierarchy<Person>> IHierarchy<Person>.Children
    ///        {
    ///            get { return Children.Cast<IHierarchy<Person>>(); }
    ///        }
    /// 
    ///        Person IHierarchy<Person>.Value { get { return this; } }
    ///
    ///        public IList<Person> Children { get; set; }            
    ///    }
    /// </code>
    /// </example>
    /// 
    /// 
    /// An alternative to implementing this interface is to use <see cref="HierarchyExtensions.AsHierarchy{T}"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IHierarchy<T>
    {
        /// <summary>
        /// Returns the collection direct descendants of this node.
        /// </summary>
        IEnumerable<IHierarchy<T>> Children { get; }

        /// <summary>
        /// The actual <typeparamref name="T"/> instance that is managed by this instance.
        /// For most implementers this will simply be a reference to the object itself.
        /// </summary>
        T Value { get; }
    }

    /// <summary>
    /// Provides extension methods for working with hierarchical data.
    /// </summary>
    public static class HierarchyExtensions
    {
        private class Hierarchy<T> : IHierarchy<T>
        {
            private Func<T, IEnumerable<T>> childGetter;

            public IEnumerable<IHierarchy<T>> Children
            {
                get
                {
                    return childGetter(Value).Select(child => child.AsHierarchy(childGetter));
                }
            }

            public T Value { get; private set; }

            public Hierarchy(T nodeValue, Func<T, IEnumerable<T>> childGetter)
            {
                Value = nodeValue;
                this.childGetter = childGetter;
            }
        }

        /// <summary>
        /// Returns an <see cref="IHierarchy{T}"/> where <see cref="IHierarchy{T}.Value"/> is <paramref name="me"/> and
        /// <see cref="IHierarchy{T}.GetChildren"/> is <paramref name="childGetter"/>
        /// </summary>
        public static IHierarchy<T> AsHierarchy<T>(this T me, Func<T, IEnumerable<T>> childGetter)
        {
            return new Hierarchy<T>(me, childGetter);
        }

        /// <summary>
        /// Returns <paramref name="me"/> and all the objects in the hierarchy
        /// below <paramref name="me"/> flattened into a sequence
        /// </summary>
        public static IEnumerable<T> Flatten<T>(this IHierarchy<T> root)
        {
            return Seq.Create(root).FlattenHierarchy(me => me.Children).Select(me => me.Value);
        }
    }
}