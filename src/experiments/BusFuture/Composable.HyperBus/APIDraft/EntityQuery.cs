using System;

// ReSharper disable All
namespace Composable.HyperBus.APIDraft
{
    public class EntityQuery<TEntity> : IQuery<TEntity>
    {
        public EntityQuery(Guid entityId) { EntityId = entityId; }
        public Guid EntityId { get; }
    }
}