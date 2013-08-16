using System;
using System.Collections.Generic;
using System.Linq;
using Composable.System.Linq;
using Composable.SystemExtensions.Threading;

namespace Composable.CQRS.EventSourcing
{
    public class InMemoryEventStore : IEventStore
    {
        private readonly ISingleContextUseGuard _threadingGuard;
        private IList<IAggregateRootEvent> _events = new List<IAggregateRootEvent>();

        public InMemoryEventStore(ISingleContextUseGuard threadingGuard)
        {
            _threadingGuard = threadingGuard;
        }

        public void Dispose()
        {
            _threadingGuard.AssertNoContextChangeOccurred(this);
        }

        public IEnumerable<IAggregateRootEvent> GetHistoryUnSafe(Guid id)
        {
            _threadingGuard.AssertNoContextChangeOccurred(this);
            return _events.Where(e => e.AggregateRootId == id);
        }

        public void SaveEvents(IEnumerable<IAggregateRootEvent> events)
        {
            _threadingGuard.AssertNoContextChangeOccurred(this);
            events.ForEach(e => _events.Add(e));
        }

        public IEnumerable<IAggregateRootEvent> StreamEventsAfterEventWithId(Guid? startAfterEventId)
        {
            _threadingGuard.AssertNoContextChangeOccurred(this);
            IEnumerable<IAggregateRootEvent> events = _events.OrderBy(e => e.TimeStamp);
            if(startAfterEventId.HasValue)
            {
                events = events.SkipWhile(e => e.EventId != startAfterEventId).Skip(1);
            }
            return events;
        }

        public void DeleteEvents(Guid aggregateId)
        {
            _threadingGuard.AssertNoContextChangeOccurred(this);
            for (int i = 0; i < _events.Count; i++)
            {
                if (_events[i].AggregateRootId == aggregateId)
                {
                    _events.RemoveAt(i);
                    i--;
                }
            }
        }

        public IEnumerable<Guid> GetAggregateIds()
        {
            _threadingGuard.AssertNoContextChangeOccurred(this);
            return _events.Select(e => e.AggregateRootId).Distinct();
        }

        public void Reset()
        {
            _threadingGuard.AssertNoContextChangeOccurred(this);
            _events = new List<IAggregateRootEvent>();
        }
    }
}