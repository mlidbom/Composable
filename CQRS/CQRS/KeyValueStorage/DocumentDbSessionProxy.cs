#region usings

using System;
using System.Collections.Generic;
using Composable.DDD;

#endregion

namespace Composable.KeyValueStorage
{
    public class DocumentDbSessionProxy : IDocumentDbSession
    {
        protected IDocumentDbSession Session { get; private set; }

        protected DocumentDbSessionProxy(IDocumentDbSession session)
        {
            Session = session;
        }

        public virtual TValue Get<TValue>(object key)
        {
            return Session.Get<TValue>(key);
        }

        public virtual bool TryGet<TValue>(object key, out TValue value)
        {
            return Session.TryGet(key, out value);
        }

        public virtual void Save<TValue>(object id, TValue value)
        {
            Session.Save(id, value);
        }

        public void Delete<TEntity>(object id)
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

        public virtual IEnumerable<T> GetAll<T>() where T : IHasPersistentIdentity<Guid>
        {
            return Session.GetAll<T>();
        }

        public virtual void Dispose()
        {
            Session.Dispose();
        }
    }
}