using System;
using System.Collections.Generic;
using Void.Linq;

namespace Void.Hierarchies
{
    /// <summary>
    /// Provides a minimal interface for representing a data type which 
    /// is hierarchical in the form that each instance has a collection 
    /// of other instances.
    /// 
    /// Implementing this interface gives access to all the extentionmethods 
    /// implemented upon it which is the main purpose of doing so.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IHierarchy<T>
    {
        /// <summary>
        /// A function that given an instance finds all the 
        /// children at the next level down in the hierarchy from that instance.
        /// 
        /// For example: A person class that has a Children collection property could 
        /// pass a method that returns that collection when given a person.
        /// 
        /// An alternative to implementing this interface is to use <see cref="HierarchyExtensions.AsHierarchy{T}"/>
        /// 
        /// </summary>
        Func<T, IEnumerable<T>> GetChildren { get; }

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
            public Func<T, IEnumerable<T>> GetChildren { get; private set; }
            public T Value { get; private set; }

            public Hierarchy(T nodeValue, Func<T, IEnumerable<T>> childGetter)
            {
                Value = nodeValue;
                GetChildren = childGetter;
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
        /// below <see cref="me"/> flattened into an <see cref="IEnumerable{T}"/>
        /// </summary>
        public static IEnumerable<T> Flatten<T>(this IHierarchy<T> me)
        {
            return Seq.Create(me.Value).FlattenHierarchy(me.GetChildren);
        }
    }
}