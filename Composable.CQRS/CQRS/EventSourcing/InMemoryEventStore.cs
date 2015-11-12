using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Composable.CQRS.EventSourcing.EventRefactoring.Migrations;
using Composable.System.Linq;

namespace Composable.CQRS.EventSourcing
{
    //todo: Refactor to use the same serialization code as the sql server event store so that tests actually tests roundtrip serialization
    public class InMemoryEventStore : IEventStore
    {
        private readonly IReadOnlyList<Func<IEventMigration>> _migrationFactories;

        private IList<IAggregateRootEvent> _events = new List<IAggregateRootEvent>();
        private int InsertionOrder;

        public void Dispose()
        {
        }

        private object _lockObject = new object();

        public InMemoryEventStore(IEnumerable<Func<IEventMigration>> migrationFactories = null )
        {
            _migrationFactories = migrationFactories?.ToList() ?? new List<Func<IEventMigration>>();
        }

        public IEnumerable<IAggregateRootEvent> GetAggregateHistory(Guid id)
        {
            lock(_lockObject)
            {
                return _events.Where(e => e.AggregateRootId == id).ToList();
            }
        }

        public void SaveEvents(IEnumerable<IAggregateRootEvent> events)
        {
            lock(_lockObject)
            {
                events.ForEach(
                    e =>
                    {
                        e.InsertionOrder = ++InsertionOrder;
                        _events.Add(e);
                    });
            }
        }

        public IEnumerable<IAggregateRootEvent> StreamEventsAfterEventWithId(Guid? startAfterEventId)
        {
            lock(_lockObject)
            {
                IEnumerable<IAggregateRootEvent> events = _events.OrderBy(e => e.TimeStamp);
                if(startAfterEventId.HasValue)
                {
                    events = events.SkipWhile(e => e.EventId != startAfterEventId).Skip(1);
                }
                return events.ToList();
            }
        }

        public void DeleteEvents(Guid aggregateId)
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

        public IEnumerable<Guid> StreamAggregateIdsInCreationOrder(Type eventBaseType = null)
        {
            Contract.Requires(eventBaseType == null || (eventBaseType.IsInterface && typeof(IAggregateRootEvent).IsAssignableFrom(eventBaseType)));

            lock (_lockObject)
            {
                return _events
                    .Where(e => eventBaseType == null || eventBaseType.IsInstanceOfType(e))
                    .OrderBy(e => e.TimeStamp)
                    .Select(e => e.AggregateRootId)
                    .Distinct()
                    .ToList();
            }
        }

        public void Reset()
        {
            lock(_lockObject)
            {
                _events = new List<IAggregateRootEvent>();
            }
        }
    }
}