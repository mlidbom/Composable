using System.Collections.Generic;

namespace Composable.CQRS.CQRS.EventSourcing
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

    interface IEntityCollectionManager<TEntity, in TEntityId, TEventClass, in TEntityCreationInterface>
    {
        IReadOnlyEntityCollection<TEntity, TEntityId> Entities { get; }
        TEntity Add<TCreationEvent>(TCreationEvent creationEvent)
            where TCreationEvent : TEventClass, TEntityCreationInterface;
    }
}
