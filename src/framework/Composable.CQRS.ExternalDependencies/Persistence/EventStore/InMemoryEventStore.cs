using System;
using System.Collections.Generic;
using System.Linq;
using Composable.Contracts;
using Composable.Messaging;
using Composable.Persistence.EventStore.Refactoring.Migrations;
using Composable.System.Linq;

namespace Composable.Persistence.EventStore
{
    //todo: Refactor to use the same serialization code as the sql server event store so that tests actually tests roundtrip serialization
    class InMemoryEventStore : IEventStore
    {
        IReadOnlyList<IEventMigration> _migrationFactories;

        IList<DomainEvent> _events = new List<DomainEvent>();
        int _insertionOrder;

        public void Dispose()
        {
        }

        readonly object _lockObject = new object();

        public InMemoryEventStore(IEnumerable<IEventMigration> migrations = null ) => _migrationFactories = migrations?.ToList() ?? new List<IEventMigration>();

        public IReadOnlyList<IDomainEvent> GetAggregateHistoryForUpdate(Guid id) => GetAggregateHistory(id);

        public IReadOnlyList<IDomainEvent> GetAggregateHistory(Guid id)
        {
            lock(_lockObject)
            {
                return SingleAggregateInstanceEventStreamMutator.MutateCompleteAggregateHistory(_migrationFactories, _events.Where(e => e.AggregateRootId == id).ToArray())
                    .ToList();
            }
        }

        public void SaveEvents(IEnumerable<IDomainEvent> events)
        {
            lock(_lockObject)
            {
                events.Cast<DomainEvent>().ForEach(
                    @event =>
                    {
                        @event.InsertionOrder = ++_insertionOrder;
                        _events.Add(@event);
                    });
            }
        }

        IEnumerable<IDomainEvent> StreamEvents()
        {
            lock(_lockObject)
            {
                var streamMutator = CompleteEventStoreStreamMutator.Create(_migrationFactories);
                return streamMutator.Mutate(_events).ToList();
            }
        }

        public void StreamEvents(int batchSize, Action<IReadOnlyList<IDomainEvent>> handleEvents)
        {
            var batches = StreamEvents()
                .ChopIntoSizesOf(batchSize)
                .Select(batch => batch.ToList());
            foreach(var batch in batches)
            {
                handleEvents(batch);
            }
        }

        public void DeleteAggregate(Guid aggregateId)
        {
            lock(_lockObject)
            {
                for(var i = 0; i < _events.Count; i++)
                {
                    if(_events[i].AggregateRootId == aggregateId)
                    {
                        _events.RemoveAt(i);
                        i--;
                    }
                }
            }
        }

        public void PersistMigrations() { _events = StreamEvents().Cast<DomainEvent>().ToList(); }

        public IEnumerable<Guid> StreamAggregateIdsInCreationOrder(Type eventBaseType = null)
        {
            OldContract.Assert.That(eventBaseType == null || eventBaseType.IsInterface && typeof(IDomainEvent).IsAssignableFrom(eventBaseType),
                                 "eventBaseType == null || eventBaseType.IsInterface && typeof(IAggregateRootEvent).IsAssignableFrom(eventBaseType)");

            lock (_lockObject)
            {
                return _events
                    .Where(e => eventBaseType == null || eventBaseType.IsInstanceOfType(e))
                    .OrderBy(e => e.UtcTimeStamp)
                    .Select(e => e.AggregateRootId)
                    .Distinct()
                    .ToList();
            }
        }

        public void TestingOnlyReplaceMigrations(IReadOnlyList<IEventMigration> migrations) { _migrationFactories = migrations; }
    }
}