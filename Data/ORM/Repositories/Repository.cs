using System.Collections.Generic;
using System.Linq;
using Void.Linq;

namespace Void.Data.ORM
{
    public class Repository<TInstance, TKey> : Repository<TInstance, TKey, IPersistanceSession>
    {
        public Repository(IPersistanceSession session) : base(session)
        {
        }
    }

    public class Repository<TInstance, TKey, TPersistenceSession> : IRepository<TInstance, TKey> where TPersistenceSession : IPersistanceSession
    {
        protected TPersistenceSession Session { get; private set; }

        public Repository(TPersistenceSession persistenceSession)
        {
            Session = persistenceSession;
        }

        public virtual TInstance Get(TKey id)
        {
            return Session.Get<TInstance>(id);
        }

        public IList<TInstance> GetAll(IEnumerable<TKey> ids)
        {
            return ids.Select(id => Get(id)).ToList();
        }

        public virtual TInstance TryGet(TKey id)
        {
            return Session.TryGet<TInstance>(id);
        }

        public virtual bool TryGet(TKey id, out TInstance result)
        {
            result = Session.TryGet<TInstance>(id);
            return result != null;
        }

        public IList<TInstance> TryGetAll(IEnumerable<TKey> ids)
        {
            return ids.Select(id => TryGet(id)).Where(instance => instance != null).ToList();
        }

        public virtual void SaveOrUpdate(TInstance instance)
        {
            Session.SaveOrUpdate(instance);
        }

        public virtual void SaveOrUpdate(IEnumerable<TInstance> instances)
        {
            instances.ForEach(SaveOrUpdate);
        }

        public virtual void Delete(TInstance instance)
        {
            Session.Delete(instance);
        }


        public IQueryable<TInstance> Find(IFilter<TInstance> criteria)
        {
            return Linq.Where(criteria);
        }

        public IQueryable<TInstance> Linq { get { return Session.Linq<TInstance>(); } }
    }
}