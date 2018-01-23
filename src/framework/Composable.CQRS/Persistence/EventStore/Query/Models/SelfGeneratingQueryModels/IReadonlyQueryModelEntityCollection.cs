using System.Collections.Generic;

namespace Composable.Persistence.EventStore.Query.Models.SelfGeneratingQueryModels
{
    public interface IReadonlyQueryModelEntityCollection<TEntity, in TEntityId> : IEnumerable<TEntity>
    {
        IReadOnlyList<TEntity> InCreationOrder { get; }
        // ReSharper disable once UnusedMember.Global todo:write test
        bool TryGet(TEntityId id, out TEntity component);
        bool Exists(TEntityId id);
        TEntity Get(TEntityId id);
        TEntity this[TEntityId id] { get; }
    }

    interface IQueryModelEntityCollectionManager<TEntity, in TEntityId>
    {
        IReadonlyQueryModelEntityCollection<TEntity, TEntityId> Entities { get; }
    }
}
