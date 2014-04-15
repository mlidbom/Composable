using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using Composable.System;

namespace Composable.CQRS.EventSourcing.SQLServer
{
    public class SqlServerEventStoreEventsCache
    {
        private static readonly ConcurrentDictionary<string, SqlServerEventStoreEventsCache> ConnectionStringToCacheMap = new ConcurrentDictionary<string, SqlServerEventStoreEventsCache>();

        private const string CacheName = "EventStore";

        private MemoryCache _internalCache = new MemoryCache(CacheName);

        private SqlServerEventStoreEventsCache()
        {
            
        }

        public static SqlServerEventStoreEventsCache ForConnectionString(string connectionString)
        {
            return ConnectionStringToCacheMap.GetOrAdd(connectionString, new SqlServerEventStoreEventsCache());
        }

        private readonly CacheItemPolicy Policy = new CacheItemPolicy()
                                                         {
                                                             //todo: this way of doing cache expiration is unlikely to be acceptable in the long run....
                                                             SlidingExpiration = 20.Minutes()
                                                         };        

        public List<IAggregateRootEvent> Get(Guid id)
        {
            var cached = _internalCache.Get(id.ToString());
            if(cached == null)
            {
                return new List<IAggregateRootEvent>();
            }
            //Make sure each caller gets their own copy.
            return ((List<IAggregateRootEvent>)cached).ToList();

        }

        public void Store(Guid id, IEnumerable<IAggregateRootEvent> events)
        {
             
            _internalCache.Set(key: id.ToString(), policy: Policy, value: events.ToList());
        }
        public void Clear()
        {
            _internalCache.Dispose();
            _internalCache = new MemoryCache(CacheName);
        }

        public void Remove(Guid id)
        {
            _internalCache.Remove(key: id.ToString());
        }
    }
}