#region usings

using System.Linq;
using NHibernate;
using NHibernate.Linq;
using Composable.Persistence;

#endregion

namespace Composable.CQRS.NHibernate
{
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
    }
}