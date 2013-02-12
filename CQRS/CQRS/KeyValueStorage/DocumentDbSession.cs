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
using Composable.System.Collections.Collections;

namespace Composable.KeyValueStorage
{
        internal static class DocumentKeyDictExtensions
    {
        public static DocumentDbSession.DocumentItem GetOrAddDefault<TDocument>(this IDictionary<DocumentDbSession.DocumentKey, DocumentDbSession.DocumentItem> me, DocumentDbSession.DocumentKey<TDocument> key)
        {
            DocumentDbSession.DocumentItem doc;
            if(!me.TryGetValue(key, out doc))
            {
                doc = new DocumentDbSession.DocumentItem(key);
                me.Add(key, doc);
            }
            return doc;
        }
    }

    public class DocumentDbSession : IDocumentDbSession, IUnitOfWorkParticipant
    {
        [ThreadStatic]
        internal static bool UseUpdateLock;

        internal readonly IObjectStore _backingStore;
        internal readonly IDocumentDbSessionInterceptor _interceptor;

        private readonly InMemoryObjectStore _idMap = new InMemoryObjectStore();
        private readonly ISingleContextUseGuard _threadingGuard;

        private readonly IDictionary<DocumentKey, DocumentItem> _knownDocuments = new Dictionary<DocumentKey, DocumentItem>(); 

        internal abstract class DocumentKey
        {
            public DocumentKey(object id, Type type)
            {
                Id = id;
                Type = type;
            }

            public bool Equals(DocumentKey other)
            {
                return Equals(Id.ToString(), other.Id.ToString()) && Equals(Type, other.Type);
            }

            public override bool Equals(object obj)
            {
                if(ReferenceEquals(null, obj))
                {
                    return false;
                }
                if(ReferenceEquals(this, obj))
                {
                    return true;
                }
                if(obj.GetType() != this.GetType())
                {
                    return false;
                }
                return Equals((DocumentKey)obj);
            }

            public override int GetHashCode()
            {
                return Id.ToString().GetHashCode();
            }

            override public string ToString()
            {
                return "Id: {0}, Type: {1}".FormatWith(Id, Type);
            }

            public object Id { get; private set; }
            public Type Type { get; private set; }

            public abstract void DeleteFromObjectStore(IObjectStore store);
        }      

        internal class DocumentKey<TDocument> : DocumentKey
        {
            public DocumentKey(object id) : base(id, typeof(TDocument)) {}
            override public void DeleteFromObjectStore(IObjectStore store)
            {
                store.Remove<TDocument>(Id);
            }
        }

        internal class DocumentItem
        {
            public DocumentKey Key { get; private set; }
            private Action<IObjectStore> _removalAction;

            public DocumentItem(DocumentKey key)
            {
                Key = key;
            }

            public Object Document { get; private set; }
            public bool IsDeleteRequested { get { return _removalAction != null; } }
            public bool IsInBackingStore { get; set; }

            public bool ScheduledForAdding { get { return !IsInBackingStore && !IsDeleteRequested; } }
            public bool ScheduledForRemoval { get { return IsInBackingStore && IsDeleteRequested; } }
            public bool ScheduledForUpdate { get { return IsInBackingStore && !IsDeleteRequested; } }

            public void RemoveFrom(IObjectStore backingStore)
            {
                if(!IsDeleteRequested)
                {
                    throw new InvalidOperationException("Deletion is not requested");
                }
                _removalAction(backingStore);
                HasBeenRemovedFromBackingStore();
            }

            public void RequestDeletion<T>(object id)
            {
                _removalAction = store =>
                                 {
                                     if(!store.Remove<T>(id))
                                     {
                                         throw new NoSuchDocumentException(id, typeof(T));
                                     }
                                 };
            }

            public void SaveRequestedForDocument<TDocument>(TDocument document)
            {
                Document = document;
                _removalAction = null;
            }

            public void DocumentLoadedFromBackingStore(object document)
            {
                Document = document;
            }

            public void HasBeenRemovedFromBackingStore()
            {
                _removalAction = _ => { };
                IsInBackingStore = false;
            }

            public void AddTo(IObjectStore backingStore)
            {
                backingStore.Add(Key, Document);
                IsInBackingStore = true;
            }
        }

        private static readonly ILog Log = LogManager.GetLogger(typeof(DocumentDbSession));

        public DocumentDbSession(IDocumentDb store, ISingleContextUseGuard singleContextUseGuard, DocumentDbConfig config = null)
        {
            _threadingGuard = singleContextUseGuard;
            if(config == null)
            {
                config = DocumentDbConfig.Default;
            }
            _backingStore = store.CreateStore();
            _interceptor = config.Interceptor;
        }


        public virtual bool TryGet<TValue>(object key, out TValue value)
        {
            _threadingGuard.AssertNoThreadChangeOccurred(this);

            if (_idMap.TryGet(key, out value))
            {
                return true;
            }

            var documentItem = _knownDocuments.GetOrAddDefault(new DocumentKey<TValue>(key));
            if (_backingStore.TryGet(key, out value) && !documentItem.IsDeleteRequested)
            {
                documentItem.DocumentLoadedFromBackingStore(value);
                if (!documentItem.IsInBackingStore)
                {
                    documentItem.IsInBackingStore = true;
                    OnInitialLoad(key, value);
                }
                return true;
            }

            return false;
        }

        private void OnInitialLoad<TValue>(object key, TValue value)
        {
            _idMap.Add(key, value);
            _knownDocuments.GetOrAddDefault(new DocumentKey<TValue>(key)).IsInBackingStore = true;
            if (_interceptor != null)
                _interceptor.AfterLoad(value);
        }

        public virtual TValue GetForUpdate<TValue>(object key)
        {
            _threadingGuard.AssertNoThreadChangeOccurred(this);
            using(new UpdateLock())
            {
                return Get<TValue>(key);
            }
        }

        public virtual bool TryGetForUpdate<TValue>(object key, out TValue value)
        {
            _threadingGuard.AssertNoThreadChangeOccurred(this);
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
            _threadingGuard.AssertNoThreadChangeOccurred(this);
            TValue value;
            if(TryGet(key, out value))
            {
                return value;
            }

            throw new NoSuchDocumentException(key, typeof(TValue));
        }

        public virtual void Save<TValue>(object id, TValue value)
        {
            _threadingGuard.AssertNoThreadChangeOccurred(this);
            if (_idMap.Contains(value.GetType(), id))
            {
                throw new AttemptToSaveAlreadyPersistedValueException(id, value);
            }
            
            var documentItem = _knownDocuments.GetOrAddDefault(new DocumentKey<TValue>(id));
            documentItem.SaveRequestedForDocument(value);

            if(_unitOfWork == null)
            {                
                _backingStore.Add(id, value);
                documentItem.IsInBackingStore = true;
            }else
            {
                Log.DebugFormat("{0} postponed persisting object from call to Save since participating in a unit of work", _id);
            }
            _idMap.Add(id, value);
        }

        public virtual void Save<TEntity>(TEntity entity) where TEntity : IHasPersistentIdentity<Guid>
        {
            _threadingGuard.AssertNoThreadChangeOccurred(this);
            Save(entity.Id, entity);
        }

        public virtual void Delete<TEntity>(TEntity entity) where TEntity : IHasPersistentIdentity<Guid>
        {
            _threadingGuard.AssertNoThreadChangeOccurred(this);
            Delete<TEntity>(entity.Id);
        }

        public virtual void Delete<T>(object id)
        {
            _threadingGuard.AssertNoThreadChangeOccurred(this);
            T ignored;
            if(!TryGet(id, out ignored))
            {
                throw new NoSuchDocumentException(id, typeof(T));
            }

            var documentItem = _knownDocuments.GetOrAddDefault(new DocumentKey<T>(id));
            documentItem.RequestDeletion<T>(id);

            if(_unitOfWork == null)
            {
                documentItem.RemoveFrom(_backingStore);
            }
            _idMap.Remove<T>(id);
        }

        public virtual void SaveChanges()
        {
            _threadingGuard.AssertNoThreadChangeOccurred(this);
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
            _knownDocuments.Where(pair => pair.Value.ScheduledForAdding).ForEach(p =>
                                                                                 {
                                                                                     p.Value.AddTo(_backingStore);
                                                                                 });

            _knownDocuments.Where(pair => pair.Value.ScheduledForRemoval).ForEach(p => p.Value.RemoveFrom(_backingStore));

            _backingStore.Update(
                _knownDocuments.Where(pair => pair.Value.ScheduledForUpdate)
                               .Select(p => new KeyValuePair<string, object>(p.Key.Id.ToString(), p.Value.Document)));
        }

        public virtual IEnumerable<T> GetAll<T>() where T : IHasPersistentIdentity<Guid>
        {
            _threadingGuard.AssertNoThreadChangeOccurred(this);
            var stored = _backingStore.GetAll<T>();
            stored.Where(pair => !_idMap.Contains(typeof (T), pair.Key))
                .ForEach(pair => OnInitialLoad(pair.Key, pair.Value));

            return _idMap.Select(pair => pair.Value).OfType<T>();
        }



        public virtual void Dispose()
        {
            _threadingGuard.AssertNoThreadChangeOccurred(this);
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