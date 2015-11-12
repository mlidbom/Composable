using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Composable.System.Collections.Collections
{
    ///<summary>Adds some convenience features to linked list</summary>
    public static class LinkedListExtensions
    {
        ///<summary>Enumerates the nodes in the linked list</summary>
        public static IEnumerable<LinkedListNode<T>> Nodes<T>(this LinkedList<T> list) { return list.First.NodesFrom(); }

        ///<summary>Enumerates this and all following nodes.</summary>
        public static IEnumerable<LinkedListNode<T>> NodesFrom<T>(this LinkedListNode<T> @this)
        {
            var node = @this;
            while(node != null)
            {
                yield return node;
                node = node.Next;
            }
        }

        ///<summary>Enumerates all following nodes excluding this node.</summary>
        public static IEnumerable<LinkedListNode<T>> NodesAfter<T>(this LinkedListNode<T> @this) { return @this.NodesFrom().Skip(1); }

        ///<summary>Enumerates this and all following node values.</summary>
        public static IEnumerable<T> ValuesFrom<T>(this LinkedListNode<T> @this) { return @this.NodesFrom().Select(node => node.Value); }

        ///<summary>Enumerates all following node values excluding this node.</summary>
        public static IEnumerable<T> ValuesAfter<T>(this LinkedListNode<T> @this) { return @this.ValuesFrom().Skip(1); }

        ///<summary>Inserts <paramref name="items"/> after the <paramref name="this"/>  node and returns the nodes that were inserted.</summary>
        public static IReadOnlyList<LinkedListNode<T>> AddAfter<T>(this LinkedListNode<T> @this, IEnumerable<T> items)
        {
            Contract.Requires(items != null);
            Contract.Requires(@this != null);

            return items
                .Reverse()
                .Select(@event => @this.List.AddAfter(@this, @event))
                .Reverse()
                .ToList();
        }

        ///<summary>Inserts <paramref name="items"/> after the <paramref name="this"/>  node and returns the nodes that were inserted.</summary>
        public static IReadOnlyList<LinkedListNode<T>> AddBefore<T>(this LinkedListNode<T> @this, IEnumerable<T> items)
        {
            Contract.Requires(items != null);
            Contract.Requires(@this != null);

            return items
                .Select(@event => @this.List.AddBefore(@this, @event))
                .ToList();
        }

        ///<summary>Replaces <paramref name="this"/> and returns the nodes that were inserted.</summary>
        public static IReadOnlyList<LinkedListNode<T>> Replace<T>(this LinkedListNode<T> @this, IEnumerable<T> items)
        {
            Contract.Requires(items != null);
            Contract.Requires(@this != null);

            var newNodes = @this.AddAfter(items);
            @this.List.Remove(@this);
            return newNodes;
        }

        ///<summary>Inserts <paramref name="items"/> after the <paramref name="this"/>  node and returns the nodes that were inserted.</summary>
        public static IReadOnlyList<LinkedListNode<T>> AddAfter<T>(this LinkedList<T> @this, LinkedListNode<T> node, IEnumerable<T> items)
        {
            Contract.Requires(items != null);
            Contract.Requires(@this != null);
            Contract.Requires(@this == node.List);

            return node.AddAfter(items);
        }

        ///<summary>Inserts <paramref name="items"/> after the <paramref name="this"/>  node and returns the nodes that were inserted.</summary>
        public static IReadOnlyList<LinkedListNode<T>> AddBefore<T>(this LinkedList<T> @this, LinkedListNode<T> node, IEnumerable<T> items)
        {
            Contract.Requires(items != null);
            Contract.Requires(@this != null);
            Contract.Requires(@this == node.List);

            return node.AddBefore(items);
        }

        ///<summary>Replaces <paramref name="this"/> and returns the nodes that were inserted.</summary>
        public static IReadOnlyList<LinkedListNode<T>> Replace<T>(this LinkedList<T> @this, LinkedListNode<T> node, IEnumerable<T> items)
        {
            Contract.Requires(items != null);
            Contract.Requires(@this != null);

            Contract.Requires(@this == node.List);
            return node.Replace(items);
        }
    }
}
