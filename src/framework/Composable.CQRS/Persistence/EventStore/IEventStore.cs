using System;
using System.Collections.Generic;

namespace Composable.Persistence.EventStore
{
    interface IEventStore : IDisposable
    {
        IReadOnlyList<IAggregateEvent> GetAggregateHistoryForUpdate(Guid id);
        IReadOnlyList<IAggregateEvent> GetAggregateHistory(Guid id);
        void SaveEvents(IEnumerable<IAggregateEvent> events);
        //todo: Utilize C# 8 asynchronous streams.
        void StreamEvents(int batchSize, Action<IReadOnlyList<IAggregateEvent>> handleEvents);
        void DeleteAggregate(Guid aggregateId);
        void PersistMigrations();

        ///<summary>The passed <paramref name="eventType"/> filters the aggregate Ids so that only ids of aggregates that are created by an event that inherits from <paramref name="eventType"/> are returned.</summary>
        IEnumerable<Guid> StreamAggregateIdsInCreationOrder(Type? eventType = null);
        void SaveEvents(EventInsertionSpecification[] events);
    }

    static class EventStoreExtensions
    {
        public static IEnumerable<Guid> StreamAggregateIdsInCreationOrder<TAggregateEvent>(this IEventStore @this) => @this.StreamAggregateIdsInCreationOrder(typeof(TAggregateEvent));
    }

    static class EventStoreTestingExtensions
    {
        public static IReadOnlyList<IAggregateEvent> ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize(this IEventStore @this, int batchSize = 10000)
        {
            var events = new List<IAggregateEvent>();
            @this.StreamEvents(batchSize, events.AddRange);
            return events;
        }
    }
}