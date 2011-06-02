using System;
using System.Collections.Generic;
using System.Transactions;
using Composable.DDD;
using Composable.System.Linq;
using System.Linq;
using Composable.System.Collections.Collections;

namespace Composable.KeyValueStorage
{
    public class InMemoryKeyValueSession : IEnlistmentNotification, IKeyValueStoreSession
    {
        private readonly InMemoryKeyValueStore _store;
        private Dictionary<Type, Dictionary<Guid, object>> _idMap = new Dictionary<Type, Dictionary<Guid, object>>();
        private bool _enlisted;

        public InMemoryKeyValueSession(InMemoryKeyValueStore store)
        {
            _store = store;
        }


        public bool TryGet<TValue>(Guid key, out TValue value)
        {
            object found;
            if (_idMap.GetOrAddDefault(typeof(TValue)).TryGetValue(key, out found))
            {
                value = (TValue)found;
                return true;
            }

            if (_store._store.GetOrAddDefault(typeof(TValue)).TryGetValue(key, out found))
            {
                _idMap.GetOrAddDefault(typeof(TValue)).Add(key, found);
                value = (TValue)found;
                return true;
            }

            value = default(TValue);
            return false;
        }

        public TValue Get<TValue>(Guid key)
        {
            TValue value;
            if(TryGet(key, out value))
            {
                return value;
            }

            throw new NoSuchKeyException(key, typeof(TValue));
        }

        public void Save<TValue>(Guid key, TValue value)
        {
            EnlistInAmbientTransaction();

            if (_idMap.GetOrAddDefault(value.GetType()).ContainsKey(key) || _store._store.GetOrAddDefault(typeof(TValue)).ContainsKey(key))
            {
                throw new AttemptToSaveAlreadyPersistedValueException(key, value);
            }
            _idMap.GetOrAddDefault(value.GetType()).Add(key, value);
        }

        public void Save<TEntity>(TEntity entity) where TEntity : IHasPersistentIdentity<Guid>
        {
            Save(entity.Id, entity);
        }

        public void Delete<TEntity>(TEntity entity) where TEntity : IHasPersistentIdentity<Guid>
        {
            _idMap.GetOrAddDefault(typeof(TEntity)).Remove(entity.Id);
            _store._store.GetOrAddDefault(typeof(TEntity)).Remove(entity.Id);
        }

        public void SaveChanges()
        {
            EnlistInAmbientTransaction();
            _idMap.ForEach(typeToContents => 
                typeToContents.Value.ForEach(
                    idToInsntance => _store._store.GetOrAddDefault(typeToContents.Key)[idToInsntance.Key] = idToInsntance.Value));            
        }

        public IEnumerable<T> GetAll<T>()
        {
            return _idMap.GetOrAddDefault(typeof(T))
                .Select(pair => pair.Value)
                .Concat(_store._store.GetOrAddDefault(typeof(T)).Select(pair => pair.Value))
                .OfType<T>().Distinct();
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
    }
}