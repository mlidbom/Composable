using System.Collections.Generic;

namespace Composable.Persistence.EventStore.Aggregates
{
    public interface IReadOnlyEntityCollection<TEntity, in TEntityId> : IEnumerable<TEntity>
    {
        IReadOnlyList<TEntity> InCreationOrder { get; }
        // ReSharper disable once UnusedMember.Global todo:write test
        bool TryGet(TEntityId id, out TEntity component);
        bool Exists(TEntityId id);
        TEntity Get(TEntityId id);
        TEntity this[TEntityId id] { get; }
    }

    interface IEntityCollectionManager<TEntity, in TEntityId, in TEventClass, in TEntityCreationInterface>
    {
        IReadOnlyEntityCollection<TEntity, TEntityId> Entities { get; }
        TEntity AddByPublishing<TCreationEvent>(TCreationEvent creationEvent) where TCreationEvent : TEventClass, TEntityCreationInterface;
    }
}
