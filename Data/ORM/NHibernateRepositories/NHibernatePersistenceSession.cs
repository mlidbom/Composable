#region usings

using System.Linq;
using NHibernate;
using NHibernate.Linq;

#endregion

namespace Composable.Data.ORM.NHibernate
{
    public class NHibernatePersistenceSession : IPersistenceSession
    {
        public NHibernatePersistenceSession(ISession session)
        {
            Session = session;
            _instances++;
        }

        public ISession Session { get; private set; }

        #region implementation of INHibernatePersistanceSession

        public void Clear()
        {
            Session.Clear();
        }

        #endregion

        #region implementation of IPersistenceSession

        public IQueryable<T> Query<T>()
        {
            return Session.Query<T>();
        }

        public T Get<T>(object id)
        {
            return Session.Load<T>(id);
        }

        public T TryGet<T>(object id)
        {
            return Session.Get<T>(id);
        }

        public void SaveOrUpdate(object instance)
        {
            Session.SaveOrUpdate(instance);
        }

        public void Delete(object instance)
        {
            Session.Delete(instance);
        }

        #endregion

        #region Implementation of IDisposable

        private static int _instances;

        ~NHibernatePersistenceSession()
        {
            if(!_disposed)
            {
                //todo:Log.For(this).ErrorMessage("{0} helper instance was not disposed!");
            }
        }

        private bool _disposed;

        public void Dispose()
        {
            _instances--;
            _disposed = true;
            Session.Dispose();
        }

        #endregion
    }
}