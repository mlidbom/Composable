using System;
using System.Collections.Generic;

namespace Composable.CQRS.EventSourcing
{
    public interface IReadOnlyAggregateRootEntityCollection<TComponent>
    {
        IReadOnlyList<TComponent> InCreationOrder { get; }
        bool TryGet(Guid id, out TComponent component);
        bool Exists(Guid id);
        TComponent Get(Guid id);
        TComponent this[Guid id] { get; }
    }
}