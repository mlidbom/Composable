using System;
using System.Collections.Generic;

namespace Composable.CQRS.EventSourcing
{
    public interface IReadOnlyEntityCollection<TEntity, in TEntityId>
    {
        IReadOnlyList<TEntity> InCreationOrder { get; }
        bool TryGet(TEntityId id, out TEntity component);
        bool Exists(TEntityId id);
        TEntity Get(TEntityId id);
        TEntity this[TEntityId id] { get; }
    }

    public interface IReadOnlyEntityCollection<TEntity> : IReadOnlyEntityCollection<TEntity, Guid>
    {
        
    }
}
