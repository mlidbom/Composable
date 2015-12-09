using System;
using System.Linq;
using Composable.Persistence.Testing;
using JetBrains.Annotations;
using NHibernate;
using NHibernate.Linq;

namespace Composable.Persistence.ORM.NHibernate
{
    [UsedImplicitly]
    public class NHibernatePersistenceSession : IPersistenceSession
    {
        private readonly ISession _session;

        public NHibernatePersistenceSession(ISession session) { _session = session; }

        public TEntity Get<TEntity>(object id) { return InternalGet<TEntity>(id); }

        public IQueryable<TEntity> Query<TEntity>() { return _session.Query<TEntity>(); }

        public bool TryGet<TEntity>(object id, out TEntity entity) { return InternalTryGet(id, out entity); }

        public TEntity GetForUpdate<TEntity>(object id) => InternalGet<TEntity>(id, LockMode.Upgrade);

        public bool TryGetForUpdate<TEntity>(object id, out TEntity model) => InternalTryGet(id, out model, LockMode.Upgrade);

        public void Save<TEntity>(TEntity entity) => _session.Save(entity);

        public void Delete<TEntity>(object id) => _session.Delete(Get<TEntity>(id));

        public void Dispose() { _session?.Dispose(); }

        private bool InternalTryGet<TEntity>(object id, out TEntity model, LockMode lockMode = null)
        {
            model = lockMode == null ? _session.Get<TEntity>(id) : _session.Get<TEntity>(id, lockMode);
            return model != null;
        }

        private TEntity InternalGet<TEntity>(object id, LockMode lockMode = null)
        {
            TEntity model;
            if(!InternalTryGet(id, out model, lockMode))
            {
                throw new NoSuchEntityException(id, typeof(TEntity));
            }
            return model;
        }
    }
}
