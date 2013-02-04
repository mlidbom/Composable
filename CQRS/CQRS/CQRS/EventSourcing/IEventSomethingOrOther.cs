using System;
using System.Collections.Generic;

namespace Composable.CQRS.EventSourcing
{
    public interface IEventSomethingOrOther : IDisposable
    {
        IEnumerable<IAggregateRootEvent> GetHistoryUnSafe(Guid id);
        void SaveEvents(IEnumerable<IAggregateRootEvent> events);
        IEnumerable<IAggregateRootEvent> StreamEventsAfterEventWithId(Guid? startAfterEventId);
        void DeleteEvents(Guid aggregateId);
        IEnumerable<Guid> GetAggregateIds();
    }
}