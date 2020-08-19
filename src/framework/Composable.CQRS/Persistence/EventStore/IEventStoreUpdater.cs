using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Composable.Persistence.EventStore
{
    public interface IEventStoreReader
    {
        IReadOnlyList<IAggregateEvent> GetHistory(Guid aggregateId);
        /// <summary>
        /// Loads a specific version of the aggregate.
        /// This instance is NOT tracked for changes.
        /// No changes to this entity vill be persisted.
        /// </summary>
        TAggregate GetReadonlyCopyOfVersion<TAggregate>(Guid aggregateId, int version) where TAggregate : IEventStored;

        TAggregate GetReadonlyCopy<TAggregate>(Guid aggregateId) where TAggregate : IEventStored;
    }

    public interface IEventStoreUpdater : IDisposable
    {
        /// <summary>
        /// Loads an aggregate and tracks it for changes.
        /// </summary>
        TAggregate Get<TAggregate>(Guid aggregateId) where TAggregate : IEventStored;

        /// <summary>
        /// Causes the store to start tracking the aggregate.
        /// </summary>
        void Save<TAggregate>(TAggregate aggregate) where TAggregate : IEventStored;

        /// <summary>
        /// Tries to get the specified instance. Returns false and sets the result to null if the aggregate did not exist.
        /// </summary>
        bool TryGet<TAggregate>(Guid aggregateId, [MaybeNullWhen(false)]out TAggregate result) where TAggregate : IEventStored;

        /// <summary>
        /// Deletes all traces of an aggregate from the store.
        /// </summary>
        void Delete(Guid aggregateId);
    }
}