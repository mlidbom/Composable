using System;
using System.Collections.Generic;
using Composable.DDD;
using Composable.KeyValueStorage.SqlServer;
using Composable.System.Linq;
using System.Linq;
using Composable.SystemExtensions.Threading;
using Composable.UnitsOfWork;
using log4net;
using Composable.System;

namespace Composable.KeyValueStorage
{
    public class DocumentDbSession : IDocumentDbSession, IUnitOfWorkParticipant
    {
        [ThreadStatic]
        internal static bool UseUpdateLock;

        internal readonly IObjectStore _backingStore;
        internal readonly IDocumentDbSessionInterceptor _interceptor;

        private readonly InMemoryObjectStore _idMap = new InMemoryObjectStore();
        private readonly InMemoryObjectStore _newlyAdded = new InMemoryObjectStore();
        private SingleThreadedUseGuard _threadingGuard;

        private static readonly ILog Log = LogManager.GetLogger(typeof(DocumentDbSession));

        public DocumentDbSession(IDocumentDb store, DocumentDbConfig config = null)
        {
            _threadingGuard = new SingleThreadedUseGuard(this);
            if(config == null)
            {
                config = DocumentDbConfig.Default;
            }
            _backingStore = store.CreateStore();
            _interceptor = config.Interceptor;
        }


        public virtual bool TryGet<TValue>(object key, out TValue value)
        {
            _threadingGuard.AssertNoThreadChangeOccurred();
            if (_idMap.TryGet(key, out value))
            {
                return true;
            }

            if (_backingStore.TryGet(key, out value))
            {
                OnInitialLoad(key, value);
                return true;
            }

            return false;
        }

        private void OnInitialLoad<TValue>(object key, TValue value)
        {
            _idMap.Add(key, value);
            if (_interceptor != null)
                _interceptor.AfterLoad(value);
        }

        public virtual TValue GetForUpdate<TValue>(object key)
        {
            _threadingGuard.AssertNoThreadChangeOccurred();
            using(new UpdateLock())
            {
                return Get<TValue>(key);
            }
        }

        public virtual bool TryGetForUpdate<TValue>(object key, out TValue value)
        {
            _threadingGuard.AssertNoThreadChangeOccurred();
            using (new UpdateLock())
            {
                return TryGet(key, out value);
            }
        }

        private class UpdateLock : IDisposable
        {
            public UpdateLock()
            {
                UseUpdateLock = true;
            }

            public void Dispose()
            {
                UseUpdateLock = false;
            }
        }

        public virtual TValue Get<TValue>(object key)
        {
            _threadingGuard.AssertNoThreadChangeOccurred();
            TValue value;
            if(TryGet(key, out value))
            {
                return value;
            }

            throw new NoSuchDocumentException(key, typeof(TValue));
        }

        public virtual void Save<TValue>(object id, TValue value)
        {
            _threadingGuard.AssertNoThreadChangeOccurred();
            if (_idMap.Contains(value.GetType(), id))
            {
                throw new AttemptToSaveAlreadyPersistedValueException(id, value);
            }
            if(_unitOfWork == null)
            {                
                _backingStore.Add(id, value); 
            }else
            {
                Log.DebugFormat("{0} postponed persisting object from call to Save since participating in a unit of work", _id);
                _newlyAdded.Add(id, value);
            }
            _idMap.Add(id, value);
        }

        public virtual void Save<TEntity>(TEntity entity) where TEntity : IHasPersistentIdentity<Guid>
        {
            _threadingGuard.AssertNoThreadChangeOccurred();
            Save(entity.Id, entity);
        }

        public virtual void Delete<TEntity>(TEntity entity) where TEntity : IHasPersistentIdentity<Guid>
        {
            _threadingGuard.AssertNoThreadChangeOccurred();
            Delete<TEntity>(entity.Id);
        }

        public virtual void Delete<T>(object id)
        {
            _threadingGuard.AssertNoThreadChangeOccurred();
            if (!_backingStore.Remove<T>(id))
            {
                throw new NoSuchDocumentException(id, typeof(T));
            }
            _idMap.Remove<T>(id);
        }

        public virtual void SaveChanges()
        {
            _threadingGuard.AssertNoThreadChangeOccurred();
            if (_unitOfWork == null)
            {                
                InternalSaveChanges();
            }else
            {
                Log.DebugFormat("{0} Ignored call to SaveChanges since participating in a unit of work", _id);
            }
        }

        private void InternalSaveChanges()
        {
            Log.DebugFormat("{0} saving changes. Unit of work: {1}",_id, _unitOfWork ?? (object)"null");
            _newlyAdded.ForEach(p => _backingStore.Add(p.Key, p.Value));
            _newlyAdded.Clear();
            _backingStore.Update(_idMap.AsEnumerable());
        }

        public virtual IEnumerable<T> GetAll<T>() where T : IHasPersistentIdentity<Guid>
        {
            _threadingGuard.AssertNoThreadChangeOccurred();
            var stored = _backingStore.GetAll<T>();
            stored.Where(pair => !_idMap.Contains(typeof (T), pair.Key))
                .ForEach(pair => OnInitialLoad(pair.Key, pair.Value));

            return _idMap.Select(pair => pair.Value).OfType<T>();
        }



        public virtual void Dispose()
        {
            _threadingGuard.AssertNoThreadChangeOccurred();
            //Can be called before the transaction commits....
            //_idMap.Clear();
        }

        public override string ToString()
        {
            return "{0}: {1}".FormatWith(_id, GetType().FullName);
        }

        private IUnitOfWork _unitOfWork;
        private readonly Guid _id = Guid.NewGuid();        


        IUnitOfWork IUnitOfWorkParticipant.UnitOfWork { get { return _unitOfWork; } }
        Guid IUnitOfWorkParticipant.Id { get { return _id; } }

        void IUnitOfWorkParticipant.Join(IUnitOfWork unit)
        {
            _unitOfWork = unit;
        }

        void IUnitOfWorkParticipant.Commit(IUnitOfWork unit)
        {
            InternalSaveChanges();
            _unitOfWork = null;
        }

        void IUnitOfWorkParticipant.Rollback(IUnitOfWork unit)
        {
            _unitOfWork = null;
        }
    }
}