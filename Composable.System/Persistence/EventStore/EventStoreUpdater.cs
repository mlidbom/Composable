using System;
using System.Collections.Generic;
using System.Linq;
using Composable.Contracts;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Buses;
using Composable.System.Linq;
using Composable.System.Reactive;
using Composable.SystemExtensions.Threading;

namespace Composable.Persistence.EventStore
{
    //Review:mlidbo: Detect and warn about using the updater within multiple transactions. That it is likely to result in optimistic concurrency exceptions.
    class EventStoreUpdater :
        IEventStoreReader,
        IEventStoreUpdater
    {
        readonly IServiceBus _bus;
        readonly IEventStore _store;
        readonly IDictionary<Guid, IEventStored> _idMap = new Dictionary<Guid, IEventStored>();
        readonly ISingleContextUseGuard _usageGuard;
        readonly List<IDisposable> _disposableResources = new List<IDisposable>();
        IUtcTimeTimeSource TimeSource { get; set; }

        public EventStoreUpdater(IServiceBus bus, IEventStore store, ISingleContextUseGuard usageGuard, IUtcTimeTimeSource timeSource)
        {
            Contract.Argument(() => bus, () => store, () => usageGuard, () => timeSource)
                        .NotNull();

            _usageGuard = usageGuard;
            _bus = bus;
            _store = store;
            TimeSource = timeSource ?? DateTimeNowTimeSource.Instance;
        }

        public TAggregate Get<TAggregate>(Guid aggregateId) where TAggregate : IEventStored
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            TAggregate result;
            if (!DoTryGet(aggregateId, out result))
            {
                throw new AggregateRootNotFoundException(aggregateId);
            }
            return result;
        }

        public bool TryGet<TAggregate>(Guid aggregateId, out TAggregate aggregate) where TAggregate : IEventStored
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            return DoTryGet(aggregateId, out aggregate);
        }

        public TAggregate LoadSpecificVersion<TAggregate>(Guid aggregateId, int version) where TAggregate : IEventStored
        {
            Contract.Assert.That(version > 0, "version > 0");

            _usageGuard.AssertNoContextChangeOccurred(this);
            var aggregate = CreateInstance<TAggregate>();
            var history = GetHistory(aggregateId);
            if (history.None())
            {
                throw new AggregateRootNotFoundException(aggregateId);
            }
            aggregate.LoadFromHistory(history.Where(e => e.AggregateRootVersion <= version));
            return aggregate;
        }

        public void Save<TAggregate>(TAggregate aggregate) where TAggregate : IEventStored
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            var changes = aggregate.GetChanges().ToList();
            if (aggregate.Version > 0 && changes.None() || changes.Any() && changes.Min(e => e.AggregateRootVersion) > 1)
            {
                throw new AttemptToSaveAlreadyPersistedAggregateException(aggregate);
            }
            if (aggregate.Version == 0 && changes.None())
            {
                throw new AttemptToSaveEmptyAggregate(aggregate);
            }

            var events = aggregate.GetChanges().ToList();
            _store.SaveEvents(events);
            events.ForEach(_bus.Publish);
            aggregate.AcceptChanges();
            _idMap.Add(aggregate.Id, aggregate);

            _disposableResources.Add(aggregate.EventStream.Subscribe(OnAggregateEvent));
        }

        void OnAggregateEvent(IAggregateRootEvent @event)
        {
            Contract.Assert.That(_idMap.ContainsKey(@event.AggregateRootId), "Got event from aggregate that is not tracked!");
            _store.SaveEvents(new[] { @event });
            _bus.Publish(@event);
        }

        public void Delete(Guid aggregateId)
        {
            _store.DeleteAggregate(aggregateId);
            _idMap.Remove(aggregateId);
        }

        public void Dispose()
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            _disposableResources.ForEach(resource => resource.Dispose());
            _store.Dispose();
        }


        public override string ToString() => $"{_id}: {GetType().FullName}";
        readonly Guid _id = Guid.NewGuid();

        public IEnumerable<IAggregateRootEvent> GetHistory(Guid aggregateId) => GetHistoryInternal(aggregateId, takeWriteLock:false);

        IEnumerable<IAggregateRootEvent> GetHistoryInternal(Guid aggregateId, bool takeWriteLock)
        {
            IList<IAggregateRootEvent> history = (takeWriteLock
                                                     ? _store.GetAggregateHistoryForUpdate(aggregateId)
                                                     : _store.GetAggregateHistory(aggregateId)).ToList();

            var version = 1;
            foreach (var aggregateRootEvent in history)
            {
                if (aggregateRootEvent.AggregateRootVersion != version++)
                {
                    throw new InvalidHistoryException(aggregateId);
                }
            }
            return history;
        }

        bool DoTryGet<TAggregate>(Guid aggregateId, out TAggregate aggregate) where TAggregate : IEventStored
        {
            IEventStored es;
            if (_idMap.TryGetValue(aggregateId, out es))
            {
                aggregate = (TAggregate)es;
                return true;
            }

            var history = GetHistoryInternal(aggregateId, takeWriteLock: true).ToList();
            if (history.Any())
            {
                aggregate = CreateInstance<TAggregate>();
                aggregate.LoadFromHistory(history);
                _idMap.Add(aggregateId, aggregate);
                _disposableResources.Add(aggregate.EventStream.Subscribe(OnAggregateEvent));
                return true;
            }
            else
            {
                aggregate = default(TAggregate);
                return false;
            }
        }

        TAggregate CreateInstance<TAggregate>() where TAggregate : IEventStored
        {
            var aggregate = (TAggregate)Activator.CreateInstance(typeof(TAggregate), nonPublic: true);
            aggregate.SetTimeSource(TimeSource);
            return aggregate;
        }
    }
}