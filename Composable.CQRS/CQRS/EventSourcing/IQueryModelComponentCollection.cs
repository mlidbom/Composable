using System;
using System.Collections.Generic;

namespace Composable.CQRS.EventSourcing
{
    public interface IQueryModelComponentCollection<TComponent, TEntityId>
    {
        IReadOnlyList<TComponent> InCreationOrder { get; }
        bool TryGet(TEntityId id, out TComponent component);
        bool Exists(TEntityId id);
        TComponent Get(TEntityId id);
    }
}