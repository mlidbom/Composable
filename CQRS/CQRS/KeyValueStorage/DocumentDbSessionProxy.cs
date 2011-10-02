#region usings

using System;
using System.Collections.Generic;
using Composable.DDD;
using Composable.UnitsOfWork;

#endregion

namespace Composable.KeyValueStorage
{
    public class DocumentDbSessionProxy : IDocumentDbSession, IUnitOfWorkParticipant
    {
        protected IDocumentDbSession Session { get; private set; }
        private IUnitOfWorkParticipant Participant { get { return (IUnitOfWorkParticipant) Session; } }

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

        #region Implementation of IUnitOfWorkParticipant

        IUnitOfWork IUnitOfWorkParticipant.UnitOfWork { get { return Participant.UnitOfWork; } }

        Guid IUnitOfWorkParticipant.Id { get { return Participant.Id; } }

        void IUnitOfWorkParticipant.Join(IUnitOfWork unit)
        {
            Participant.Join(unit);
        }

        void IUnitOfWorkParticipant.Commit(IUnitOfWork unit)
        {
            Participant.Commit(unit);
        }

        void IUnitOfWorkParticipant.Rollback(IUnitOfWork unit)
        {
            Participant.Rollback(unit);
        }

        #endregion
    }
}