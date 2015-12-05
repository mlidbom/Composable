using System;
using System.Collections.Generic;

namespace Composable.CQRS.EventSourcing
{
    public interface IEventStore : IDisposable
    {
        IEnumerable<IAggregateRootEvent> GetAggregateHistory(Guid id);
        void SaveEvents(IEnumerable<IAggregateRootEvent> events);
        IEnumerable<IAggregateRootEvent> StreamEvents();
        void DeleteEvents(Guid aggregateId);
        void PersistMigrations();
        IEnumerable<Guid> StreamAggregateIdsInCreationOrder(Type eventBaseType = null);
        [Obsolete("No longer supported. Use StreamEvents()")]
        IEnumerable<IAggregateRootEvent> StreamEventsAfterEventWithId(Guid? startAfterEventId);
    }
}