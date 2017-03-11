using System.Collections;
using System.Collections.Generic;

namespace Composable.CQRS.CQRS.EventSourcing
{
    public class EntityCollection<TEntity, TEntityId> : IReadOnlyEntityCollection<TEntity, TEntityId>
    {
        readonly Dictionary<TEntityId, TEntity> _entities = new Dictionary<TEntityId, TEntity>();
        readonly List<TEntity> _entitiesInCreationOrder = new List<TEntity>();


        public IReadOnlyList<TEntity> InCreationOrder => _entitiesInCreationOrder;

        public bool TryGet(TEntityId id, out TEntity component) => _entities.TryGetValue(id, out component);
        public bool Exists(TEntityId id) => _entities.ContainsKey(id);
        public TEntity Get(TEntityId id) => _entities[id];
        public TEntity this[TEntityId id] => _entities[id];

        internal void Remove(TEntityId id)
        {
            var toRemove = _entities[id];
            _entities.Remove(id);
            _entitiesInCreationOrder.Remove(toRemove);
        }

        internal void Add(TEntity entity, TEntityId id)
        {
            _entities.Add(id, entity);
            _entitiesInCreationOrder.Add(entity);
        }

        public IEnumerator<TEntity> GetEnumerator() => _entitiesInCreationOrder.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _entitiesInCreationOrder.GetEnumerator();
    }
}