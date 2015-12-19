using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

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


    public interface IEntityCollectionManager<TEntity, in TEntityId, TEventClass, in TEntityCreationInterface>
    {
        IReadOnlyEntityCollection<TEntity, TEntityId> Entities { get; }
        TEntity Add<TCreationEvent>(TCreationEvent creationEvent)
            where TCreationEvent : TEventClass, TEntityCreationInterface;
    }

    public interface IEntityCollectionManager<TEntity, TEventClass, TEntityCreationInterface> : IEntityCollectionManager<TEntity, Guid, TEventClass, TEntityCreationInterface>
    {

    }


    public class EntityCollection<TEntity, TEntityId> : IReadOnlyEntityCollection<TEntity, TEntityId>
    {
        private readonly Dictionary<TEntityId, TEntity> _entities = new Dictionary<TEntityId, TEntity>();
        private readonly List<TEntity> _entitiesInCreationOrder = new List<TEntity>();


        public IReadOnlyList<TEntity> InCreationOrder => _entitiesInCreationOrder;

        [Pure]
        public bool TryGet(TEntityId id, out TEntity component) => _entities.TryGetValue(id, out component);        
        public bool Exists(TEntityId id) => _entities.ContainsKey(id);
        public TEntity Get(TEntityId id) => _entities[id];
        public TEntity this[TEntityId id] => _entities[id];

        public void Remove(TEntityId id)
        {
            var toRemove = _entities[id];
            _entities.Remove(id);
            _entitiesInCreationOrder.Remove(toRemove);
        }

        public void Add(TEntity entity, TEntityId id)
        {
            _entities.Add(id, entity);
            _entitiesInCreationOrder.Add(entity);
        }        
    }


}
