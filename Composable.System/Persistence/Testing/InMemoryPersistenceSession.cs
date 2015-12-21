using System;
using System.Collections.Generic;
using System.Linq;
using Composable.DDD;
using JetBrains.Annotations;

namespace Composable.Persistence.Testing
{
    ///<summary>A simple collection based implementation of <see cref="IPersistenceSession"/></summary>
    [UsedImplicitly]
    public class InMemoryPersistenceSession : IPersistenceSession
    {
        private HashSet<object> _db = new HashSet<object>();

        ///<summary>See interface documentation</summary>
        public void Dispose()
        {
            _db = null;
        }

        ///<summary>See interface documentation</summary>
        public TEntity Get<TEntity>(object id)
        {
            TEntity result;
            if(!TryGet(id, out result))
            {
                throw new NoSuchEntityException(id, typeof(TEntity));
            }
            return result;
        }

        ///<summary>See interface documentation</summary>
        public IQueryable<TEntity> Query<TEntity>() => _db.OfType<TEntity>().AsQueryable();

        ///<summary>See interface documentation</summary>
        public bool TryGet<TEntity>(object id, out TEntity entity)
        {
            entity = _db.OfType<IHasPersistentIdentity<Guid>>()
                .Where(instance => instance.Id == (Guid)id)
                .Cast<TEntity>()
                .SingleOrDefault();
            return entity != null;
        }

        ///<summary>See interface documentation</summary>
        public void Save<TEntity>(TEntity entity)
        {
            _db.Add(entity);
        }

        ///<summary>See interface documentation</summary>
        public void Delete<TEntity>(object id)
        {
            _db.Remove(Get<TEntity>(id));
        }

        ///<summary>See interface documentation</summary>
        public TEntity GetForUpdate<TEntity>(object id) => Get<TEntity>(id);

        ///<summary>See interface documentation</summary>
        public bool TryGetForUpdate<TEntity>(object id, out TEntity model) => TryGet(id, out model);
    }

    ///<summary>Thrown if no entity with a given id could be found.</summary>
    public class NoSuchEntityException : Exception
    {
        ///<summary>Constructs an instance of the exception using the supplied parameters to create a usefull exception message.</summary>
        public NoSuchEntityException(object id, Type type) : base($"Could not find an entity with id: {id} and type: {type.FullName}") { }
    }
}
