using System;
using System.Collections.Generic;

namespace Composable.Persistence.EventStore
{
    interface IEventStore : IDisposable
    {
        IReadOnlyList<IAggregateRootEvent> GetAggregateHistoryForUpdate(Guid id);
        IReadOnlyList<IAggregateRootEvent> GetAggregateHistory(Guid id);
        void SaveEvents(IEnumerable<IAggregateRootEvent> events);
        void StreamEvents(int batchSize, Action<IReadOnlyList<IAggregateRootEvent>> handleEvents);
        void DeleteAggregate(Guid aggregateId);
        void PersistMigrations();

        ///<summary>The passed <paramref name="eventBaseType"/> filters the aggregate Ids so that only ids of aggregates that are created by an event that inherits from <paramref name="eventBaseType"/> are returned.</summary>
        IEnumerable<Guid> StreamAggregateIdsInCreationOrder(Type eventBaseType = null);
    }

    static class EventStoreExtensions
    {
        public static IEnumerable<Guid> StreamAggregateIdsInCreationOrder<TBaseAggregateEventInterface>(this IEventStore @this) => @this.StreamAggregateIdsInCreationOrder(typeof(TBaseAggregateEventInterface));
    }

    static class EventStoreTestingExtensions
    {
        public static IReadOnlyList<IAggregateRootEvent> ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize(this IEventStore @this)
        {
            var events = new List<IAggregateRootEvent>();
            @this.StreamEvents(10000, events.AddRange);
            return events;
        }
    }
}