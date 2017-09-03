using System;
using System.Collections.Generic;

using System.Linq;

namespace Composable.System.Collections.Collections
{
    ///<summary>Adds some convenience features to linked list</summary>
    static class LinkedListExtensions
    {
        ///<summary>Enumerates this and all following nodes.</summary>
        static IEnumerable<LinkedListNode<T>> NodesFrom<T>(this LinkedListNode<T> @this)
        {
            var node = @this;
            while(node != null)
            {
                yield return node;
                node = node.Next;
            }
        }

        ///<summary>Enumerates this and all following node values.</summary>
        public static IEnumerable<T> ValuesFrom<T>(this LinkedListNode<T> @this) { return @this.NodesFrom().Select(node => node.Value); }

        ///<summary>Inserts <paramref name="items"/> after the <paramref name="this"/>  node and returns the nodes that were inserted.</summary>
        public static void AddBefore<T>(this LinkedListNode<T> @this, IEnumerable<T> items)
        {
            if(items == null || @this == null)
            {
                throw new ArgumentNullException();
            }

            foreach(var item in items)
            {
                @this.List.AddBefore(@this, item);
            }
        }

        ///<summary>Replaces <paramref name="this"/> and returns the nodes that were inserted.</summary>
        public static LinkedListNode<T> Replace<T>(this LinkedListNode<T> @this, IEnumerable<T> items)
        {
            if (items == null || @this == null)
            {
                throw new ArgumentNullException();
            }

            LinkedListNode<T> current = null;
            var newItemsReversed = items.Reverse().ToList();
            if(newItemsReversed.Count < 1)
            {
                throw new ArgumentException($"{nameof(items)} may not be empty");
            }

            foreach(var newItem in newItemsReversed)
            {
                current = @this.List.AddAfter(@this, newItem);
            }
            @this.List.Remove(@this);
            return current;
        }


    }
}
