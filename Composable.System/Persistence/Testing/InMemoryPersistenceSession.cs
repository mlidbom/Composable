using System;
using System.Collections.Generic;
using System.Linq;
using Composable.DDD;
using JetBrains.Annotations;

namespace Composable.Persistence.Testing
{
    [UsedImplicitly]
    public class InMemoryPersistenceSession : IPersistenceSession
    {
        private HashSet<object> _db = new HashSet<object>();

        public void Dispose()
        {
            _db = null;
        }

        public TEntity Get<TEntity>(object id)
        {
            TEntity result;
            if(!TryGet(id, out result))
            {
                throw new NoSuchEntityException(id, typeof(TEntity));
            }
            return result;
        }
        public IQueryable<TEntity> Query<TEntity>() => _db.OfType<TEntity>().AsQueryable();

        public bool TryGet<TEntity>(object id, out TEntity entity)
        {
            entity = _db.OfType<IHasPersistentIdentity<Guid>>()
                .Where(instance => instance.Id == (Guid)id)
                .Cast<TEntity>()
                .SingleOrDefault();
            return entity != null;
        }

        public void Save<TEntity>(TEntity entity)
        {
            _db.Add(entity);
        }

        public void Delete<TEntity>(object id)
        {
            _db.Remove(Get<TEntity>(id));
        }

        public TEntity GetForUpdate<TEntity>(object id) => Get<TEntity>(id);
        public bool TryGetForUpdate<TEntity>(object id, out TEntity model) => TryGet(id, out model);
    }


    public class NoSuchEntityException : Exception
    {
        public NoSuchEntityException(object id, Type type) : base($"Could not find an entity with id: {id} and type: {type.FullName}") { }
    }
}
