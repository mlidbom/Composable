using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Composable.Contracts;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Buses.Implementation;
using Composable.SystemCE.LinqCE;
using Composable.SystemCE.ReactiveCE;
using Composable.SystemCE.ReflectionCE;
using Composable.SystemCE.ThreadingCE;

namespace Composable.Persistence.EventStore
{
    class EventStoreUpdater : IEventStoreReader, IEventStoreUpdater
    {
        readonly IEventStoreEventPublisher _eventStoreEventPublisher;
        readonly IEventStore _store;
        readonly IAggregateTypeValidator _aggregateTypeValidator;
        readonly IDictionary<Guid, IEventStored> _idMap = new Dictionary<Guid, IEventStored>();
        readonly ISingleContextUseGuard _usageGuard;
        readonly List<IDisposable> _disposableResources = new List<IDisposable>();
        IUtcTimeTimeSource TimeSource { get; set; }

        public EventStoreUpdater(IEventStoreEventPublisher eventStoreEventPublisher, IEventStore store, IUtcTimeTimeSource timeSource, IAggregateTypeValidator aggregateTypeValidator)
        {
            Contract.ArgumentNotNull(eventStoreEventPublisher, nameof(eventStoreEventPublisher), store, nameof(store), timeSource, nameof(timeSource));

            _usageGuard = new CombinationUsageGuard(new SingleThreadUseGuard(), new SingleTransactionUsageGuard());
            _eventStoreEventPublisher = eventStoreEventPublisher;
            _store = store;
            _aggregateTypeValidator = aggregateTypeValidator;
            TimeSource = timeSource;
        }

        public TAggregate Get<TAggregate>(Guid aggregateId) where TAggregate : IEventStored
        {
            _aggregateTypeValidator.AssertIsValid<TAggregate>();
            _usageGuard.AssertNoContextChangeOccurred(this);
            if (!DoTryGet(aggregateId, out TAggregate result))
            {
                throw new AggregateNotFoundException(aggregateId);
            }
            return result!;
        }

        public bool TryGet<TAggregate>(Guid aggregateId, [MaybeNullWhen(false)]out TAggregate aggregate) where TAggregate : IEventStored
        {
            _aggregateTypeValidator.AssertIsValid<TAggregate>();
            _usageGuard.AssertNoContextChangeOccurred(this);
            return DoTryGet(aggregateId, out aggregate);
        }

        public TAggregate GetReadonlyCopy<TAggregate>(Guid aggregateId) where TAggregate : IEventStored => LoadSpecificVersionInternal<TAggregate>(aggregateId, int.MaxValue, verifyVersion: false);

        public TAggregate GetReadonlyCopyOfVersion<TAggregate>(Guid aggregateId, int version) where TAggregate : IEventStored => LoadSpecificVersionInternal<TAggregate>(aggregateId, version);

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        TAggregate LoadSpecificVersionInternal<TAggregate>(Guid aggregateId, int version, bool verifyVersion = true) where TAggregate : IEventStored
        {
            _aggregateTypeValidator.AssertIsValid<TAggregate>();
            Contract.Assert.That(version > 0, "version > 0");

            _usageGuard.AssertNoContextChangeOccurred(this);

            var history = GetHistory(aggregateId);
            if(history.None())
            {
                throw new AggregateNotFoundException(aggregateId);
            }

            if(verifyVersion && history.Count < version - 1)
            {
                throw new Exception($"Requested version: {version} not found. Current version: {history.Count}");
            }

            var aggregate = CreateInstance<TAggregate>();
            aggregate.LoadFromHistory(history.Where(e => e.AggregateVersion <= version));
            return aggregate;
        }

        public void Save<TAggregate>(TAggregate aggregate) where TAggregate : IEventStored
        {
            _aggregateTypeValidator.AssertIsValid<TAggregate>();
            _usageGuard.AssertNoContextChangeOccurred(this);

            aggregate.Commit(events =>
            {
                if (aggregate.Version > 0 && events.None() || events.Any() && events.Min(e => e.AggregateVersion) > 1)
                {
                    throw new AttemptToSaveAlreadyPersistedAggregateException(aggregate);
                }
                if (aggregate.Version == 0 && events.None())
                {
                    throw new AttemptToSaveEmptyAggregateException(aggregate);
                }

                _store.SaveSingleAggregateEvents(events);

                events.ForEach(_eventStoreEventPublisher.Publish);
            });

            _idMap.Add(aggregate.Id, aggregate);

            _disposableResources.Add(aggregate.EventStream.Subscribe(OnAggregateEvent));
        }

        void OnAggregateEvent(IAggregateEvent @event)
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            if(!_idMap.ContainsKey(@event.AggregateId))
            {
                throw new Exception($"Got event from aggregate that is not tracked! Id: {@event.AggregateId}");
            }
            _store.SaveSingleAggregateEvents(new[] { @event });
            _eventStoreEventPublisher.Publish(@event);
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

        public IReadOnlyList<IAggregateEvent> GetHistory(Guid aggregateId) => GetHistoryInternal(aggregateId, takeWriteLock:false);

        IReadOnlyList<IAggregateEvent> GetHistoryInternal(Guid aggregateId, bool takeWriteLock) =>
            takeWriteLock
                ? _store.GetAggregateHistoryForUpdate(aggregateId)
                : _store.GetAggregateHistory(aggregateId);

        bool DoTryGet<TAggregate>(Guid aggregateId, [MaybeNullWhen(false)]out TAggregate aggregate) where TAggregate : IEventStored
        {
            if (_idMap.TryGetValue(aggregateId, out var eventStored))
            {
                aggregate = (TAggregate)eventStored;
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
                aggregate = default!;
                return false;
            }
        }

        TAggregate CreateInstance<TAggregate>() where TAggregate : IEventStored
        {
            var aggregate = Constructor.For<TAggregate>.DefaultConstructor.Instance();
            aggregate.SetTimeSource(TimeSource);
            return aggregate;
        }
    }
}