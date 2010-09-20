using System;
using System.Linq;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Linq;
using NHibernate.Tool.hbm2ddl;

namespace Void.Data.ORM.NHibernate
{
    public abstract class NHibernatePersistenceSession : INHibernatePersistenceSession
    {

        private IInterceptor _interceptor;
        private ISession _session;

        protected NHibernatePersistenceSession(IInterceptor interceptor)
        {
            _interceptor = interceptor;
        }

        private ISession Session
        {
            get
            {
                if (_session == null)
                {
                    if (_interceptor != null)
                    {
                        _session = SessionFactory.OpenSession(_interceptor);
                    }
                    else
                    {
                        _session = SessionFactory.OpenSession();
                    }
                }
                return _session;
            }
        }
        protected abstract ISessionFactory SessionFactory { get; }
        protected abstract Configuration Configuration { get; }     

        #region implementation of INHibernatePersistanceSession

        public void CreateDataBase()
        {
            new SchemaExport(Configuration).Execute(false, true, false, Session.Connection, null);
        }

        public IQuery CreateQuery(string query)
        {
            return Session.CreateQuery(query);
        }

        public void Clear()
        {
            Session.Clear();
        }

        public void Evict(object instance)
        {
            Session.Evict(instance);
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
        protected NHibernatePersistenceSession()
        {
            _instances++;
        }

        ~NHibernatePersistenceSession()
        {
            if (!_disposed)
            {
                //todo:Log.For(this).ErrorMessage("{0} helper instance was not disposed!");
            }
        }

        private bool _disposed;

        public void Dispose()
        {
            _instances--;
            _disposed = true;
            if (_session != null)
            {
                _session.Dispose();
            }
        }

        #endregion

    }
}