using System;
using System.Collections.Generic;

namespace Composable.CQRS.EventSourcing
{
    public interface IQueryModelComponentCollection<TComponent>
    {
        IReadOnlyList<TComponent> InCreationOrder { get; }
        bool TryGet(Guid id, out TComponent component);
        bool Exists(Guid id);
        TComponent Get(Guid id);
    }
}