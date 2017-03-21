using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using Composable.Persistence.EventSourcing;
using Composable.System;

namespace Composable.Persistence.EventStore.MicrosoftSQLServer
{
    class SqlServerEventStoreEventsCache
    {
        const string CacheName = "EventStore";

        MemoryCache _internalCache = new MemoryCache(CacheName);

        static readonly CacheItemPolicy Policy = new CacheItemPolicy()
                                                         {
                                                             //todo: this way of doing cache expiration is unlikely to be acceptable in the long run....
                                                             SlidingExpiration = 20.Minutes()
                                                         };

        public Entry GetCopy(Guid id)
        {
            var cached = (Entry)_internalCache.Get(id.ToString());
            if(cached == null)
            {
                return Entry.Empty;
            }
            //Make sure each caller gets their own copy.
            return new Entry(events: cached.Events.ToList(), maxSeenInsertedVersion: cached.MaxSeenInsertedVersion);
        }

        public void Store(Guid id, Entry entry)
        {
            _internalCache.Set(key: id.ToString(), policy: Policy, value: entry);
        }

        public void Clear()
        {
            _internalCache.Dispose();
            _internalCache = new MemoryCache(CacheName);
        }

        public void Remove(Guid id) { _internalCache.Remove(key: id.ToString()); }

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
