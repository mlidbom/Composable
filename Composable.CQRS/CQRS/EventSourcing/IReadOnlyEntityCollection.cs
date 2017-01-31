using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace Composable.CQRS.EventSourcing
{
    public interface IReadOnlyEntityCollection<TEntity, in TEntityId> : IEnumerable<TEntity>
    {
        [Pure]
        IReadOnlyList<TEntity> InCreationOrder { get; }
        // ReSharper disable once UnusedMember.Global todo:write test
        bool TryGet(TEntityId id, out TEntity component);
        [Pure]
        bool Exists(TEntityId id);
        [Pure]
        TEntity Get(TEntityId id);
        [Pure]
        TEntity this[TEntityId id] { get; }
    }

    public interface IEntityCollectionManager<TEntity, in TEntityId, TEventClass, in TEntityCreationInterface>
    {
        IReadOnlyEntityCollection<TEntity, TEntityId> Entities { get; }
        TEntity Add<TCreationEvent>(TCreationEvent creationEvent)
            where TCreationEvent : TEventClass, TEntityCreationInterface;
    }
}
