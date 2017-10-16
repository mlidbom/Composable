using System;
using System.Collections.Generic;
using Composable.Messaging;

namespace Composable.Persistence.EventStore
{
    interface IEventStore : IDisposable
    {
        IReadOnlyList<IDomainEvent> GetAggregateHistoryForUpdate(Guid id);
        IReadOnlyList<IDomainEvent> GetAggregateHistory(Guid id);
        void SaveEvents(IEnumerable<IDomainEvent> events);
        void StreamEvents(int batchSize, Action<IReadOnlyList<IDomainEvent>> handleEvents);
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
        public static IReadOnlyList<IDomainEvent> ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize(this IEventStore @this, int batchSize = 10000)
        {
            var events = new List<IDomainEvent>();
            @this.StreamEvents(batchSize, events.AddRange);
            return events;
        }
    }
}