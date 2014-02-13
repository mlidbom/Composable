using System;
using System.Collections.Generic;
using System.Linq;
using Composable.DDD;

namespace Composable.Persistence
{
    public class InMemoryPersistenceSession : IPersistenceSession
    {
        private HashSet<object> _db = new HashSet<object>();
        public void Dispose()
        {
            _db = null;
        }

        public IQueryable<T> Query<T>()
        {
            return _db.OfType<T>().AsQueryable();
        }

        public T Get<T>(object id)
        {
            return _db.OfType<IPersistentEntity<Guid>>().Where(entity => entity.Id == (Guid)id).Cast<T>().Single();
        }

        private static bool ValuesEqual(dynamic instance, dynamic id)
        {
            return instance.Id == id;
        }

        public T TryGet<T>(object id)
        {
            return _db.OfType<IPersistentEntity<Guid>>().Where(entity => entity.Id == (Guid)id).Cast<T>().SingleOrDefault();
        }

        public void Save(object instance)
        {
            _db.Add(instance);
        }

        public void Delete(object instance)
        {
            _db.Remove(instance);
        }
    }
}