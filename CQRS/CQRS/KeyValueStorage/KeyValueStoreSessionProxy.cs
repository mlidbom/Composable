#region usings

using System;
using System.Collections.Generic;
using Composable.DDD;

#endregion

namespace Composable.KeyValueStorage
{
    public class KeyValueStoreSessionProxy : IKeyValueStoreSession
    {
        protected IKeyValueStoreSession Session { get; private set; }

        protected KeyValueStoreSessionProxy(IKeyValueStoreSession session)
        {
            Session = session;
        }

        public virtual TValue Get<TValue>(Guid key)
        {
            return Session.Get<TValue>(key);
        }

        public virtual bool TryGet<TValue>(Guid key, out TValue value)
        {
            return Session.TryGet(key, out value);
        }

        public virtual void Save<TValue>(Guid id, TValue value)
        {
            Session.Save(id, value);
        }

        public void Delete<TEntity>(Guid id)
        {
            Session.Delete<TEntity>(id);
        }

        public virtual void Save<TEntity>(TEntity entity) where TEntity : IHasPersistentIdentity<Guid>
        {
            Session.Save(entity);
        }

        public virtual void Delete<TEntity>(TEntity entity) where TEntity : IHasPersistentIdentity<Guid>
        {
            Session.Delete(entity);
        }

        public virtual void SaveChanges()
        {
            Session.SaveChanges();
        }

        public virtual IEnumerable<T> GetAll<T>()
        {
            return Session.GetAll<T>();
        }

        public virtual void Dispose()
        {
            Session.Dispose();
        }
    }
}