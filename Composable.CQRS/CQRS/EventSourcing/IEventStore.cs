using System;
using System.Collections.Generic;

namespace Composable.CQRS.EventSourcing
{
    public interface IEventStore : IDisposable
    {
        IEnumerable<IAggregateRootEvent> GetAggregateHistory(Guid id, bool takeReadLock = false);
        void SaveEvents(IEnumerable<IAggregateRootEvent> events);
        void StreamEvents(int batchSize, Action<IReadOnlyList<IAggregateRootEvent>> handleEvents);
        void DeleteEvents(Guid aggregateId);
        [Obsolete("Absolutely not ready to be used in production. DO NOT USE IN PRODUCTION!")]
        void PersistMigrations();

        ///<summary>The passed <paramref name="eventBaseType"/> filters the aggregate Ids so that only ids of aggregates that are created by an event that inherits from <paramref name="eventBaseType"/> are returned.</summary>
        IEnumerable<Guid> StreamAggregateIdsInCreationOrder(Type eventBaseType = null);
    }

    public static class EventStoreExtensions
    {
        public static IEnumerable<Guid> StreamAggregateIdsInCreationOrder<TBaseAggregateEventInterface>(this IEventStore @this)
        {
            return @this.StreamAggregateIdsInCreationOrder(typeof(TBaseAggregateEventInterface));
        }
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