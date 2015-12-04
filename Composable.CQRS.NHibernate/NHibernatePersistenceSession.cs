#region usings

using System;
using System.Linq;
using NHibernate;
using NHibernate.Linq;
using Composable.Persistence;

#endregion

namespace Composable.CQRS.NHibernate
{
    [Obsolete("This entire Nuget package is obsolete. Please uninstall and install Composable.Persistence.ORM.NHibernate instead", error:true)]
    public class NHibernatePersistenceSession : IPersistenceSession
    {
        public NHibernatePersistenceSession(ISession session)
        {
            Session = session;
        }

        public ISession Session { get; private set; }

        public IQueryable<T> Query<T>()
        {
            return Session.Query<T>();
        }

        public T Get<T>(object id)
        {
            return Session.Load<T>(id);
        }
        public bool TryGet<TEntity>(object id, out TEntity entity) { throw new global::System.NotImplementedException(); }

        public void Save(object instance)
        {
            Session.Save(instance);
        }

        public void Delete(object instance)
        {
            Session.Delete(instance);
        }

        public void Clear()
        {
            Session.Clear();
        }


        #region Implementation of IDisposable


        public void Dispose()
        {
            Session.Dispose();
        }

        #endregion

        public TEntity GetForUpdate<TEntity>(object id) { throw new global::System.NotImplementedException(); }
        public bool TryGetForUpdate<TEntity>(object id, out TEntity model) { throw new global::System.NotImplementedException(); }
        public void Save<TEntity>(TEntity entity) { throw new global::System.NotImplementedException(); }
        public void Delete<TEntity>(object id) { throw new global::System.NotImplementedException(); }
    }
}