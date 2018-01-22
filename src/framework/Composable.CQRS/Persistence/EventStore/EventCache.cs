using System;
using System.Collections.Generic;
using System.Transactions;
using Composable.System;
using Composable.System.Threading.ResourceAccess;
using Composable.SystemExtensions.TransactionsCE;
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
        class TransactionalOverlay
        {
            readonly EventCache _parent;
            readonly object _lock = new object();

            readonly IThreadShared<Dictionary<string, Dictionary<Guid, Entry>>> _overlays = ThreadShared<Dictionary<string, Dictionary<Guid, Entry>>>.Optimized();

            Dictionary<Guid, Entry> CurrentOverlay
            {
                get
                {
                    var transactionId = Transaction.Current.TransactionInformation.LocalIdentifier;
                    Dictionary<Guid, Entry> overlay = null;

                    if(_overlays.WithExclusiveAccess(@this => @this.TryGetValue(transactionId, out overlay)))
                    {
                        return overlay;
                    }

                    overlay = new Dictionary<Guid, Entry>();

                    _overlays.WithExclusiveAccess(@this => @this.Add(transactionId, overlay));

                    Transaction.Current.OnCommittedSuccessfully(() => _parent.AcceptTransactionResult(overlay));
                    Transaction.Current.OnCompleted(() => _overlays.WithExclusiveAccess(@this => @this.Remove(transactionId)));

                    return overlay;
                }
            }

            public TransactionalOverlay(EventCache eventCache) => _parent = eventCache;

            internal void Add(Guid aggregateId, Entry entry)
            {
                lock(_lock)
                {
                    CurrentOverlay[aggregateId] = entry;
                }
            }

            internal bool TryGet(Guid aggregateId, out Entry entry)
            {
                entry = null;
                if(Transaction.Current == null) return false;
                lock(_lock)
                {
                    return CurrentOverlay.TryGetValue(aggregateId, out entry);
                }
            }
        }

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

        TransactionalOverlay _transactionalOverlay;

        public EventCache() => Reset();

        void AcceptTransactionResult(Dictionary<Guid, Entry> overlay)
        {
            foreach(var entry in overlay)
            {
                StoreInternal(entry.Key, entry.Value);
            }
        }

        public Entry Get(Guid id)
        {
            if(_transactionalOverlay.TryGet(id, out var entry))
            {
                return entry;
            }

            return GetInternal(id) ?? Entry.Empty;
        }

        public void Store(Guid id, Entry entry)
        {
            if(Transaction.Current != null)
            {
                _transactionalOverlay.Add(id, entry);
            } else
            {
                StoreInternal(id, entry);
            }
        }

        public void Remove(Guid id) => RemoveInternal(id);

#if NET461
        const string CacheName = "EventStore";

        MemoryCache _internalCache;

        static readonly CacheItemPolicy Policy = new CacheItemPolicy
                                                 {
                                                     SlidingExpiration = 20.Minutes()
                                                 };

        void Reset()
        {
            _internalCache = new MemoryCache(CacheName);
            _transactionalOverlay = new TransactionalOverlay(this);
        }

        void StoreInternal(Guid id, Entry value) => _internalCache.Set(key: id.ToString(), policy: Policy, value: value);
        void RemoveInternal(Guid id) => _internalCache.Remove(key: id.ToString());
        Entry GetInternal(Guid id) => (Entry)_internalCache.Get(id.ToString());

        public void Clear()
        {
            var originalCache = _internalCache;
            Reset();
            originalCache.Dispose();
        }
#endif

#if NETSTANDARD2_0
        MemoryCache _internalCache;

        static readonly MemoryCacheEntryOptions Policy = new MemoryCacheEntryOptions()
                                                 {
                                                     SlidingExpiration = 20.Minutes()
                                                 };

        void StoreInternal(Guid id, Entry entry) => _internalCache.Set(key: id.ToString(), value: entry, options: Policy);
        Entry GetInternal(Guid id) => (Entry)_internalCache.Get(id.ToString());
        void RemoveInternal(Guid id) => _internalCache.Remove(key: id.ToString());

        void Reset()
        {
            _internalCache = new MemoryCache(new MemoryCacheOptions());
            _transactionalOverlay = new TransactionalOverlay(this);
        }

        public void Clear()
        {
            var originalCache = _internalCache;
            _internalCache = new MemoryCache(new MemoryCacheOptions()) {};
            originalCache.Dispose();
        }
#endif
    }
}
