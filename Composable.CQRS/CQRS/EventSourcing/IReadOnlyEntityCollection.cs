using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace Composable.CQRS.EventSourcing
{
    public interface IReadOnlyEntityCollection<TEntity, in TEntityId> : IEnumerable<TEntity>
    {
        [Pure]
        IReadOnlyList<TEntity> InCreationOrder { get; }
        bool TryGet(TEntityId id, out TEntity component);
        [Pure]
        bool Exists(TEntityId id);
        [Pure]
        TEntity Get(TEntityId id);
        [Pure]
        TEntity this[TEntityId id] { get; }
    }



    public interface IReadOnlyEntityCollection<TEntity> : IReadOnlyEntityCollection<TEntity, Guid>
    {
        
    }


    public interface IEntityCollectionManager<TEntity, in TEntityId, TEventClass, in TEntityCreationInterface>
    {
        IReadOnlyEntityCollection<TEntity, TEntityId> Entities { get; }
        TEntity Add<TCreationEvent>(TCreationEvent creationEvent)
            where TCreationEvent : TEventClass, TEntityCreationInterface;
    }

    public interface IEntityCollectionManager<TEntity, TEventClass, TEntityCreationInterface> : IEntityCollectionManager<TEntity, Guid, TEventClass, TEntityCreationInterface>
    {

    }    
}
