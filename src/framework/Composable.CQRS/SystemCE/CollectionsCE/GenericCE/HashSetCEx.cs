using System.Collections.Generic;
using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace Composable.SystemCE.CollectionsCE.GenericCE
{
    interface IReadonlySetCEx<TItem> : IReadOnlyCollection<TItem>
    {
        bool Contains(TItem item);

        bool IsProperSubsetOf(IEnumerable<TItem> other);

        bool IsProperSupersetOf(IEnumerable<TItem> other);

        bool IsSubsetOf(IEnumerable<TItem> other);

        bool IsSupersetOf(IEnumerable<TItem> other);

        bool Overlaps(IEnumerable<TItem> other);

        bool SetEquals(IEnumerable<TItem> other);
    }

    class HashSetCEx<TItem> : HashSet<TItem>, IReadonlySetCEx<TItem>
    {
        public HashSetCEx() {}
        public HashSetCEx([NotNull] IEnumerable<TItem> collection) : base(collection) {}
        public HashSetCEx([NotNull] IEnumerable<TItem> collection, IEqualityComparer<TItem> comparer) : base(collection, comparer) {}
        public HashSetCEx(IEqualityComparer<TItem> comparer) : base(comparer) {}
        public HashSetCEx(int capacity) : base(capacity) {}
        public HashSetCEx(int capacity, IEqualityComparer<TItem> comparer) : base(capacity, comparer) {}
        protected HashSetCEx(SerializationInfo info, StreamingContext context) : base(info, context) {}
    }
}
