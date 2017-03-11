using System;
using System.Collections.Generic;
using Composable.CQRS.EventSourcing;
using JetBrains.Annotations;

namespace Composable.CQRS.CQRS.EventSourcing
{
    public interface IEventStoreReader
    {
        IEnumerable<IAggregateRootEvent> GetHistory(Guid aggregateId);
    }

    [UsedImplicitly]
    public interface IEventStoreSession : IDisposable
    {
        /// <summary>
        /// Loads an aggregate and tracks it for changes.
        /// </summary>
        TAggregate Get<TAggregate>(Guid aggregateId) where TAggregate : IEventStored;

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

        /// <summary>
        /// Tries to get the specified instance. Returns false and sets the result to null if the aggregate did not exist.
        /// </summary>
        bool TryGet<TAggregate>(Guid aggregateId, out TAggregate result) where TAggregate : IEventStored;

        /// <summary>
        /// Deletes all traces of an aggregate from the store.
        /// </summary>
        void Delete(Guid aggregateId);
    }
}