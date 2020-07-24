using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Composable.Persistence.EventStore.Query.Models.SelfGeneratingQueryModels
{
    public interface IReadonlyQueryModelEntityCollection<TEntity, in TEntityId> : IEnumerable<TEntity>
    {
        IReadOnlyList<TEntity> InCreationOrder { get; }
        bool TryGet(TEntityId id, [MaybeNullWhen(false)]out TEntity component);
        bool Exists(TEntityId id);
        TEntity Get(TEntityId id);
        TEntity this[TEntityId id] { get; }
    }
}
