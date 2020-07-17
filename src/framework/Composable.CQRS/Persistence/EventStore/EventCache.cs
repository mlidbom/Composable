using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Transactions;
using Composable.Contracts;
using Composable.Persistence.EventStore.PersistenceLayer;
using Composable.System;
using Composable.System.Threading.ResourceAccess;
using Composable.SystemExtensions.TransactionsCE;

using Microsoft.Extensions.Caching.Memory;

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
                    Dictionary<Guid, Entry>? overlay = null;

                    if(_overlays.WithExclusiveAccess(@this => @this.TryGetValue(transactionId, out overlay)))
                    {
                        return Assert.Result.NotNull(overlay);
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

            internal bool TryGet(Guid aggregateId, [NotNullWhen(true)]out Entry? entry)
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
            public static readonly Entry Empty = new Entry();
            Entry()
            {
                Events = Array.Empty<AggregateEvent>();
                MaxSeenInsertedVersion = 0;
            }

            public IReadOnlyList<AggregateEvent> Events { get; private set; }
            public int MaxSeenInsertedVersion { get; private set; }
            public int InsertedVersionToAggregateVersionOffset { get; }

            public Entry(IReadOnlyList<AggregateEvent> events, int maxSeenInsertedVersion)
            {
                Events = events;
                MaxSeenInsertedVersion = maxSeenInsertedVersion;
                InsertedVersionToAggregateVersionOffset = MaxSeenInsertedVersion - events[^1].AggregateVersion;
            }

            public EventInsertionSpecification CreateInsertionSpecificationForNewEvent(IAggregateEvent @event)
            {
                if(InsertedVersionToAggregateVersionOffset > 0)
                {
                    return new EventInsertionSpecification(@event: @event,
                                                           insertedVersion: @event.AggregateVersion + InsertedVersionToAggregateVersionOffset,
                                                           effectiveVersion:@event.AggregateVersion);
                } else
                {
                    return new EventInsertionSpecification(@event:@event);
                }
            }
        }

        readonly TransactionalOverlay _transactionalOverlay;

        public EventCache()
        {
            _internalCache = new MemoryCache(new MemoryCacheOptions());
            _transactionalOverlay = new TransactionalOverlay(this);
        }

        void AcceptTransactionResult(Dictionary<Guid, Entry> overlay)
        {
            foreach(var (key, value) in overlay)
            {
                StoreInternal(key, value);
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

        MemoryCache _internalCache;

        static readonly MemoryCacheEntryOptions Policy = new MemoryCacheEntryOptions
                                                         {
                                                     SlidingExpiration = 20.Minutes()
                                                 };

        void StoreInternal(Guid id, Entry entry) => _internalCache.Set(key: id.ToString(), value: entry, options: Policy);
        Entry? GetInternal(Guid id) => (Entry?)_internalCache.Get(id.ToString());
        void RemoveInternal(Guid id) => _internalCache.Remove(key: id.ToString());

        public void Clear()
        {
            var originalCache = _internalCache;
            _internalCache = new MemoryCache(new MemoryCacheOptions()) {};
            originalCache.Dispose();
        }
    }
}
