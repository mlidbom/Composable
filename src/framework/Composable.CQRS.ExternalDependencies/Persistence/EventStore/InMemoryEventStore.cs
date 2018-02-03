using System;
using System.Collections.Generic;
using System.Linq;
using Composable.Contracts;
using Composable.Persistence.EventStore.Refactoring.Migrations;
using Composable.System.Linq;

namespace Composable.Persistence.EventStore
{
    //refactor: to use the same serialization code as the sql server event store so that tests actually tests roundtrip serialization
    class InMemoryEventStore : IEventStore
    {
        IReadOnlyList<IEventMigration> _migrationFactories;

        IList<AggregateEvent> _events = new List<AggregateEvent>();
        int _insertionOrder;

        public void Dispose()
        {
        }

        readonly object _lockObject = new object();

        public InMemoryEventStore(IEnumerable<IEventMigration> migrations = null ) => _migrationFactories = migrations?.ToList() ?? new List<IEventMigration>();

        public IReadOnlyList<IAggregateEvent> GetAggregateHistoryForUpdate(Guid id) => GetAggregateHistory(id);

        public IReadOnlyList<IAggregateEvent> GetAggregateHistory(Guid id)
        {
            lock(_lockObject)
            {
                return SingleAggregateInstanceEventStreamMutator.MutateCompleteAggregateHistory(_migrationFactories, _events.Where(e => e.AggregateId == id).ToArray())
                    .ToList();
            }
        }

        public void SaveEvents(IEnumerable<IAggregateEvent> events)
        {
            lock(_lockObject)
            {
                events.Cast<AggregateEvent>().ForEach(
                    @event =>
                    {
                        @event.InsertionOrder = ++_insertionOrder;
                        _events.Add(@event);
                    });
            }
        }

        IEnumerable<IAggregateEvent> StreamEvents()
        {
            lock(_lockObject)
            {
                var streamMutator = CompleteEventStoreStreamMutator.Create(_migrationFactories);
                return streamMutator.Mutate(_events).ToList();
            }
        }

        public void StreamEvents(int batchSize, Action<IReadOnlyList<IAggregateEvent>> handleEvents)
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
                    if(_events[i].AggregateId == aggregateId)
                    {
                        _events.RemoveAt(i);
                        i--;
                    }
                }
            }
        }

        public void PersistMigrations() { _events = StreamEvents().Cast<AggregateEvent>().ToList(); }

        public IEnumerable<Guid> StreamAggregateIdsInCreationOrder(Type eventType = null)
        {
            Contract.Assert.That(eventType == null || eventType.IsInterface && typeof(IAggregateEvent).IsAssignableFrom(eventType),
                                 "eventBaseType == null || eventBaseType.IsInterface && typeof(IAggregateEvent).IsAssignableFrom(eventType)");

            lock (_lockObject)
            {
                return _events
                    .Where(e => eventType == null || eventType.IsInstanceOfType(e))
                    .OrderBy(e => e.UtcTimeStamp)
                    .Select(e => e.AggregateId)
                    .Distinct()
                    .ToList();
            }
        }

        public void TestingOnlyReplaceMigrations(IReadOnlyList<IEventMigration> migrations) { _migrationFactories = migrations; }
    }
}