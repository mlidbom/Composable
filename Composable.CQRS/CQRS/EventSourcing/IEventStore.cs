using System;
using System.Collections.Generic;

namespace Composable.CQRS.EventSourcing
{
    public interface IEventStore : IDisposable
    {
        IEnumerable<IAggregateRootEvent> GetAggregateHistory(Guid id);
        void SaveEvents(IEnumerable<IAggregateRootEvent> events);
        void StreamEvents(int batchSize, Action<IReadOnlyList<IAggregateRootEvent>> handleEvents);
        void DeleteEvents(Guid aggregateId);
        [Obsolete("Absolutely not ready to be used in production. DO NOT USE IN PRODUCTION!")]
        void PersistMigrations();
        IEnumerable<Guid> StreamAggregateIdsInCreationOrder(Type eventBaseType = null);
    }

    public static class EventStoreTestingExtensions
    {
        public static IReadOnlyList<IAggregateRootEvent> ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize(this IEventStore @this)
        {
            var events = new List<IAggregateRootEvent>();
            @this.StreamEvents(10000, events.AddRange);
            return events;
        }
    }
}