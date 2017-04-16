using System;
using System.Collections.Generic;
using System.Runtime.Caching;
using Composable.System;

namespace Composable.Persistence.EventStore
{
    class EventCache
    {
        const string CacheName = "EventStore";

        readonly MemoryCache _internalCache = new MemoryCache(CacheName);

        static readonly CacheItemPolicy Policy = new CacheItemPolicy
                                                 {
                                                     SlidingExpiration = 20.Minutes()
                                                 };

        public Entry Get(Guid id) => (Entry)_internalCache.Get(id.ToString()) ?? Entry.Empty;

        public void Store(Guid id, Entry entry) => _internalCache.Set(key: id.ToString(), policy: Policy, value: entry);

        public void Remove(Guid id) => _internalCache.Remove(key: id.ToString());

        internal class Entry
        {
            public static readonly  Entry Empty = new Entry(new List<AggregateRootEvent>(), 0);

            public IReadOnlyList<AggregateRootEvent> Events { get; private set; }
            public int MaxSeenInsertedVersion { get; private set; }

            public Entry(IReadOnlyList<AggregateRootEvent> events, int maxSeenInsertedVersion)
            {
                Events = events;
                MaxSeenInsertedVersion = maxSeenInsertedVersion;
            }
        }
    }
}
