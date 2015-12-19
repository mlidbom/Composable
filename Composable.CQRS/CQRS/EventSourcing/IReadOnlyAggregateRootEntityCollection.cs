using System;
using System.Collections.Generic;

namespace Composable.CQRS.EventSourcing
{
    public interface IReadOnlyAggregateRootEntityCollection<TEntity,TEntityId>
    {
        IReadOnlyList<TEntity> InCreationOrder { get; }
        bool TryGet(TEntityId id, out TEntity component);
        bool Exists(TEntityId id);
        TEntity Get(TEntityId id);
        TEntity this[TEntityId id] { get; }
    }

    public interface IReadOnlyAggregateRootEntityCollection<TEntity> : IReadOnlyAggregateRootEntityCollection<TEntity, Guid>
    {        
    }
}