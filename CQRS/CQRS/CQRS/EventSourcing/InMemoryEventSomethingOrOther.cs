using System;
using System.Collections.Generic;
using System.Linq;
using Composable.System.Linq;

namespace Composable.CQRS.EventSourcing
{
    public class InMemoryEventSomethingOrOther : IEventSomethingOrOther
    {
        private readonly InMemoryEventStore _store;

        public InMemoryEventSomethingOrOther(InMemoryEventStore store)
        {
            _store = store;
        }

        public void Dispose()
        {
        }

        public IEnumerable<IAggregateRootEvent> GetHistoryUnSafe(Guid id)
        {
            return _store.Events.Where(e => e.AggregateRootId == id);
        }

        public void SaveEvents(IEnumerable<IAggregateRootEvent> events)
        {
            events.ForEach(e => _store.Events.Add(e));
        }
    }
}