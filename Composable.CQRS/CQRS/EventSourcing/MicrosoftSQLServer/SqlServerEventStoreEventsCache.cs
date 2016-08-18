using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using Composable.System;
using Composable.System.Linq;

namespace Composable.CQRS.EventSourcing.MicrosoftSQLServer
{
    public class SqlServerEventStoreEventsCache
    {
        private static readonly ConcurrentDictionary<string, SqlServerEventStoreEventsCache> ConnectionStringToCacheMap =
            new ConcurrentDictionary<string, SqlServerEventStoreEventsCache>();

        private const string CacheName = "EventStore";

        private MemoryCache _internalCache = new MemoryCache(CacheName);

        private SqlServerEventStoreEventsCache() { }

        public static SqlServerEventStoreEventsCache ForConnectionString(string connectionString)
        {
            return ConnectionStringToCacheMap.GetOrAdd(connectionString, key => new SqlServerEventStoreEventsCache());
        }

        private static readonly CacheItemPolicy Policy = new CacheItemPolicy()
                                                         {
                                                             //todo: this way of doing cache expiration is unlikely to be acceptable in the long run....
                                                             SlidingExpiration = 20.Minutes()
                                                         };

        public IReadOnlyList<AggregateRootEvent> GetCopy(Guid id)
        {
            var cached = _internalCache.Get(id.ToString());
            if(cached == null)
            {
                return new List<AggregateRootEvent>();
            }
            //Make sure each caller gets their own copy.
            return ((List<AggregateRootEvent>)cached).ToList();
        }

        public void Store(Guid id, IEnumerable<AggregateRootEvent> events)
        {
            _internalCache.Set(key: id.ToString(), policy: Policy, value: events.ToList());
        }

        public void Clear()
        {
            _internalCache.Dispose();
            _internalCache = new MemoryCache(CacheName);
        }

        public static void ClearAll()
        {
            ConnectionStringToCacheMap.Values.ForEach(@this => @this.Clear());
        }

        public void Remove(Guid id) { _internalCache.Remove(key: id.ToString()); }
    }
}
