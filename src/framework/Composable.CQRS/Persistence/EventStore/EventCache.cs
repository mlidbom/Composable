using System;
using System.Collections.Generic;
using Composable.System;


#if NET461
using System.Runtime.Caching;
#endif

#if NETSTANDARD2_0
    using Microsoft.Extensions.Caching.Memory;
#endif

namespace Composable.Persistence.EventStore
{
    class EventCache
    {
#if NET461
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
            public static readonly Entry Empty = new Entry(new List<AggregateRootEvent>(), 0);

            public IReadOnlyList<AggregateRootEvent> Events                 { get; private set; }
            public int                               MaxSeenInsertedVersion { get; private set; }

            public Entry(IReadOnlyList<AggregateRootEvent> events, int maxSeenInsertedVersion)
            {
                Events                 = events;
                MaxSeenInsertedVersion = maxSeenInsertedVersion;
            }
        }
#endif

#if NETSTANDARD2_0
       readonly MemoryCache _internalCache = new MemoryCache(new MemoryCacheOptions())
                                              {
                                              };

        static readonly MemoryCacheEntryOptions Policy = new MemoryCacheEntryOptions()
                                                 {
                                                     SlidingExpiration = 20.Minutes()
                                                 };

        public Entry Get(Guid id) => (Entry)_internalCache.Get(id.ToString()) ?? Entry.Empty;

        public void Store(Guid id, Entry entry) => _internalCache.Set(key: id.ToString(), value: entry, options: Policy);

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
#endif
    }
}
