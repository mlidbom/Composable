﻿using System;
using System.Collections.Generic;
using System.Linq;
using Composable.System.Linq;
using Composable.SystemExtensions.Threading;

namespace Composable.CQRS.EventSourcing
{
    public class InMemoryEventSomethingOrOther : IEventSomethingOrOther
    {
        private readonly InMemoryEventStore _store;
        private readonly ISingleContextUseGuard _threadingGuard;

        public InMemoryEventSomethingOrOther(InMemoryEventStore store, ISingleContextUseGuard threadingGuard)
        {
            _threadingGuard = threadingGuard;
            _store = store;
        }

        public void Dispose()
        {
            _threadingGuard.AssertNoThreadChangeOccurred(this);
        }

        public IEnumerable<IAggregateRootEvent> GetHistoryUnSafe(Guid id)
        {
            _threadingGuard.AssertNoThreadChangeOccurred(this);
            return _store.Events.Where(e => e.AggregateRootId == id);
        }

        public void SaveEvents(IEnumerable<IAggregateRootEvent> events)
        {
            _threadingGuard.AssertNoThreadChangeOccurred(this);
            events.ForEach(e => _store.Events.Add(e));
        }

        public IEnumerable<IAggregateRootEvent> StreamEventsAfterEventWithId(Guid? startAfterEventId)
        {
            _threadingGuard.AssertNoThreadChangeOccurred(this);
            IEnumerable<IAggregateRootEvent> events = _store.Events.OrderBy(e => e.TimeStamp);
            if(startAfterEventId.HasValue)
            {
                events = events.SkipWhile(e => e.EventId != startAfterEventId).Skip(1);
            }
            return events;
        }

        public void DeleteEvents(Guid aggregateId)
        {
            _threadingGuard.AssertNoThreadChangeOccurred(this);
            for (int i = 0; i < _store.Events.Count; i++)
            {
                if (_store.Events[i].AggregateRootId == aggregateId)
                {
                    _store.Events.RemoveAt(i);
                    i--;
                }
            }
        }
    }
}