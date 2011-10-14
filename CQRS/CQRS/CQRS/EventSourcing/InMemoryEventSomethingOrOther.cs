using System;
using System.Collections.Generic;
using System.Linq;
using Composable.System.Linq;
using Composable.SystemExtensions.Threading;

namespace Composable.CQRS.EventSourcing
{
    public class InMemoryEventSomethingOrOther : IEventSomethingOrOther
    {
        private readonly InMemoryEventStore _store;
        private readonly SingleThreadedUseGuard _threadingGuard;

        public InMemoryEventSomethingOrOther(InMemoryEventStore store)
        {
            _threadingGuard = new SingleThreadedUseGuard(this);
            _store = store;
        }

        public void Dispose()
        {
            _threadingGuard.AssertNoThreadChangeOccurred();
        }

        public IEnumerable<IAggregateRootEvent> GetHistoryUnSafe(Guid id)
        {
            _threadingGuard.AssertNoThreadChangeOccurred();
            return _store.Events.Where(e => e.AggregateRootId == id);
        }

        public void SaveEvents(IEnumerable<IAggregateRootEvent> events)
        {
            _threadingGuard.AssertNoThreadChangeOccurred();
            events.ForEach(e => _store.Events.Add(e));
        }

        public IEnumerable<IAggregateRootEvent> StreamEventsAfterEventWithId(Guid? startAfterEventId)
        {
            _threadingGuard.AssertNoThreadChangeOccurred();
            IEnumerable<IAggregateRootEvent> events = _store.Events.OrderBy(e => e.TimeStamp);
            if(startAfterEventId.HasValue)
            {
                events = events.SkipWhile(e => e.EventId != startAfterEventId).Skip(1);
            }
            return events;
        }
    }
}