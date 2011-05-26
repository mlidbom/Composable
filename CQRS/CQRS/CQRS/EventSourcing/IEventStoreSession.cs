using System;

namespace Composable.CQRS.EventSourcing
{
    public interface IEventStoreSession : IDisposable
    {
        /// <summary>
        /// Loads an aggregate and tracks it for changes.
        /// </summary>
        TAggregate Load<TAggregate>(Guid aggregateId) where TAggregate : IEventStored;

        /// <summary>
        /// Loads a specific version of the aggregate. 
        /// This instance is NOT tracked for changes. 
        /// No changes to this entity vill be persisted.
        /// </summary>
        TAggregate LoadSpecificVersion<TAggregate>(Guid aggregateId, int version) where TAggregate : IEventStored;

        /// <summary>
        /// Causes the store to start tracking the aggregate.
        /// </summary>
        void Save<TAggregate>(TAggregate aggregate) where TAggregate : IEventStored;

        /// <summary>
        /// Detects and persist all uncommited events in tracked aggregates.
        /// </summary>
        void SaveChanges();
    }
}