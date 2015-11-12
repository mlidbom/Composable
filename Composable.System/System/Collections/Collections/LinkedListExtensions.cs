using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Composable.System.Collections.Collections
{
    ///<summary>Adds some convenience features to linked list</summary>
    public static class LinkedListExtensions
    {
        ///<summary>Enumerates the nodes in the linked list</summary>
        public static IEnumerable<LinkedListNode<T>> Nodes<T>(this LinkedList<T> list)
        {
            var node = list.First;
            while (node != null)
            {
                yield return node;
                node = node.Next;
            }
        }

        ///<summary>Inserts <paramref name="items"/> after the <paramref name="this"/>  node and returns the nodes that were inserted.</summary>
        public static IReadOnlyList<LinkedListNode<T>> AddAfter<T>(this LinkedListNode<T> @this, IEnumerable<T> items)
        {
            return items
                 .Reverse()
                 .Select(@event => @this.List.AddAfter(@this, @event))
                 .Reverse()
                 .ToList();
        }

        ///<summary>Inserts <paramref name="items"/> after the <paramref name="this"/>  node and returns the nodes that were inserted.</summary>
        public static IReadOnlyList<LinkedListNode<T>> AddBefore<T>(this LinkedListNode<T> @this, IEnumerable<T> items)
        {
            return items
                 .Select(@event => @this.List.AddBefore(@this, @event))
                 .ToList();
        }


        ///<summary>Replaces <paramref name="this"/> and returns the nodes that were inserted.</summary>
        public static IReadOnlyList<LinkedListNode<T>> Replace<T>(this LinkedListNode<T> @this, IEnumerable<T> items)
        {
            var newNodes = @this.AddAfter(items);
            @this.List.Remove(@this);
            return newNodes;
        }


        ///<summary>Inserts <paramref name="items"/> after the <paramref name="this"/>  node and returns the nodes that were inserted.</summary>
        public static IReadOnlyList<LinkedListNode<T>> AddAfter<T>(this LinkedList<T> @this, LinkedListNode<T> node, IEnumerable<T> items)
        {
            Contract.Requires(@this == node.List);
            return node.AddAfter(items);
        }

        ///<summary>Inserts <paramref name="items"/> after the <paramref name="this"/>  node and returns the nodes that were inserted.</summary>
        public static IReadOnlyList<LinkedListNode<T>> AddBefore<T>(this LinkedList<T> @this, LinkedListNode<T> node, IEnumerable<T> items)
        {
            Contract.Requires(@this == node.List);
            return node.AddBefore(items);
        }


        ///<summary>Replaces <paramref name="this"/> and returns the nodes that were inserted.</summary>
        public static IReadOnlyList<LinkedListNode<T>> Replace<T>(this LinkedList<T> @this, LinkedListNode<T> node, IEnumerable<T> items)
        {
            Contract.Requires(@this == node.List);
            return node.Replace(items);
        }

    }
}