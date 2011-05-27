using System;
using System.Collections.Generic;
using System.Transactions;
using Composable.DDD;
using Composable.System.Linq;

namespace Composable.KeyValueStorage
{
    public class InMemoryKeyValueSession : IEnlistmentNotification, IKeyValueSession
    {
        private readonly InMemoryKeyValueStore _store;
        private Dictionary<Guid, object> _idMap = new Dictionary<Guid, object>();
        private bool _enlisted;

        public InMemoryKeyValueSession(InMemoryKeyValueStore store)
        {
            _store = store;
        }

        public TValue Get<TValue>(Guid key)
        {
            object value;
            if(_idMap.TryGetValue(key, out value))
            {
                return (TValue)value;
            }

            if(_store._store.TryGetValue(key, out value))
            {
                _idMap.Add(key, value);
                return (TValue)value;
            }

            throw new NoSuchKeyException(key);
        }

        public void Save<TValue>(Guid key, TValue value)
        {
            EnlistInAmbientTransaction();

            if(_idMap.ContainsKey(key) || _store._store.ContainsKey(key))
            {
                throw new AttemptToSaveAlreadyPersistedValueException(key, value);
            }
            _idMap.Add(key, value);
        }

        public void Save<TEntity>(TEntity entity) where TEntity : IPersistentEntity<Guid>
        {
            Save(entity.Id, entity);
        }

        public void SaveChanges()
        {
            EnlistInAmbientTransaction();
            _idMap.ForEach(entry => _store._store[entry.Key] = entry.Value);            
        }

        private void EnlistInAmbientTransaction()
        {
            if (Transaction.Current != null && !_enlisted)
            {
                Transaction.Current.EnlistVolatile(this, EnlistmentOptions.None);
                _enlisted = true;
            }
        }

        public void Dispose()
        {
            //Can be called before the transaction commits....
            //_idMap.Clear();
        }

        void IEnlistmentNotification.Prepare(PreparingEnlistment preparingEnlistment)
        {
            SaveChanges();
            preparingEnlistment.Prepared();
        }

        void IEnlistmentNotification.Commit(Enlistment enlistment)
        {
            _enlisted = false;
            enlistment.Done();
        }

        void IEnlistmentNotification.Rollback(Enlistment enlistment)
        {
            _enlisted = false;
        }

        public void InDoubt(Enlistment enlistment)
        {
            _enlisted = false;
            enlistment.Done();
        }
    }
}