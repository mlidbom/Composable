using System;
using System.Collections.Generic;
using System.Linq;
using Composable.DDD;
using Composable.Persistence.KeyValueStorage;
using Composable.System;
using Composable.System.Linq;
using Composable.SystemExtensions.Threading;
using Composable.UnitsOfWork;
using log4net;

namespace Composable.DocumentDb
{
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    partial class DocumentDbSession : IDocumentDbSession, IUnitOfWorkParticipant
    {
        [ThreadStatic]
        internal static bool UseUpdateLock;

        readonly InMemoryObjectStore _idMap = new InMemoryObjectStore();

        readonly IDocumentDb _backingStore;
        readonly ISingleContextUseGuard _usageGuard;

        readonly IDictionary<DocumentKey, DocumentItem> _handledDocuments = new Dictionary<DocumentKey, DocumentItem>();

        static readonly ILog Log = LogManager.GetLogger(typeof(DocumentDbSession));

        //Review:mlidbo: Always requiring an interceptor causes a lot of unneeded complexity for clients. Consider creating a virtual void OnFirstLoad(T document) method instead. This would allow for inheriting this class to create "interceptable" sessions. Alternatively maybe an observable/event could be used somehow.
        public DocumentDbSession(IDocumentDb backingStore, ISingleContextUseGuard usageGuard)
        {
            _usageGuard = usageGuard;
            _backingStore = backingStore;
        }

        public virtual bool TryGet<TValue>(object key, out TValue document) => TryGetInternal(key, typeof(TValue), out document);

        bool TryGetInternal<TValue>(object key, Type documentType, out TValue value)
        {
            if(documentType.IsInterface)
            {
                throw new ArgumentException("You cannot query by id for an interface type. There is no guarantee of uniqueness");
            }
            _usageGuard.AssertNoContextChangeOccurred(this);

            if (_idMap.TryGet(key, out value) && documentType.IsAssignableFrom(value.GetType()))
            {
                return true;
            }

            var documentItem = GetDocumentItem(key, documentType);
            if(!documentItem.IsDeleted && _backingStore.TryGet(key, out value, _persistentValues) && documentType.IsAssignableFrom(value.GetType()))
            {
                OnInitialLoad(key, value);
                return true;
            }

            return false;
        }

        DocumentItem GetDocumentItem(object key, Type documentType)
        {
            DocumentItem doc;
            var documentKey = new DocumentKey(key, documentType);

            if (!_handledDocuments.TryGetValue(documentKey, out doc))
            {
                doc = new DocumentItem(documentKey, _backingStore, _persistentValues);
                _handledDocuments.Add(documentKey, doc);
            }
            return doc;
        }

        void OnInitialLoad(object key, object value)
        {
            _idMap.Add(key, value);
            GetDocumentItem(key, value.GetType()).DocumentLoadedFromBackingStore(value);
        }

        public virtual TValue GetForUpdate<TValue>(object key)
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            using(new UpdateLock())
            {
                return Get<TValue>(key);
            }
        }

        public virtual bool TryGetForUpdate<TValue>(object key, out TValue value)
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            using (new UpdateLock())
            {
                return TryGet(key, out value);
            }
        }

        class UpdateLock : IDisposable
        {
            public UpdateLock() => UseUpdateLock = true;

            public void Dispose()
            {
                UseUpdateLock = false;
            }
        }


        public IEnumerable<TValue> Get<TValue>(IEnumerable<Guid> ids) where TValue : IHasPersistentIdentity<Guid>
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            var idSet = ids.ToSet();//Avoid multiple enumerations.

            var stored = _backingStore.GetAll<TValue>(idSet);

            stored.Where(document => !_idMap.Contains(typeof(TValue), document.Id))
                .ForEach(unloadedDocument => OnInitialLoad(unloadedDocument.Id, unloadedDocument));

            var results = _idMap.Select(pair => pair.Value).OfType<TValue>().Where(candidate => idSet.Contains(candidate.Id)).ToArray();
            var missingDocuments = idSet.Where(id => !results.Any(result => result.Id == id)).ToArray();
            if (missingDocuments.Any())
            {
                throw new NoSuchDocumentException(missingDocuments.First(), typeof(TValue));
            }
            return results;
        }

        public virtual TValue Get<TValue>(object key)
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            TValue value;
            if(TryGet(key, out value))
            {
                return value;
            }

            throw new NoSuchDocumentException(key, typeof(TValue));
        }

        public virtual void Save<TValue>(object id, TValue value)
        {
            _usageGuard.AssertNoContextChangeOccurred(this);

            TValue ignored;
            if (TryGetInternal(id, value.GetType(), out ignored))
            {
                throw new AttemptToSaveAlreadyPersistedValueException(id, value);
            }

            var documentItem = GetDocumentItem(id, value.GetType());
            documentItem.Save(value);

            _idMap.Add(id, value);
            if(_unitOfWork == null)
            {
                documentItem.CommitChangesToBackingStore();
            }else
            {
                Log.DebugFormat("{0} postponed persisting object from call to Save since participating in a unit of work", _id);
            }
        }

        public virtual void Save<TEntity>(TEntity entity) where TEntity : IHasPersistentIdentity<Guid>
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            if(entity.Id.Equals(Guid.Empty))
            {
                throw new DocumentIdIsEmptyGuidException();
            }
            Save(entity.Id, entity);
        }

        public virtual void Delete<TEntity>(TEntity entity) where TEntity : IHasPersistentIdentity<Guid>
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            Delete<TEntity>(entity.Id);
        }

        public virtual void Delete<T>(object id)
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            T ignored;
            if(!TryGet(id, out ignored))
            {
                throw new NoSuchDocumentException(id, typeof(T));
            }

            var documentItem = GetDocumentItem(id, typeof(T));
            documentItem.Delete();

            _idMap.Remove<T>(id);
            if(_unitOfWork == null)
            {
                documentItem.CommitChangesToBackingStore();
            }
            else
            {
                Log.DebugFormat("{0} postponed deleting object since participating in a unit of work", _id);
            }
        }

        public virtual void SaveChanges()
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            if (_unitOfWork == null)
            {
                InternalSaveChanges();
            }else
            {
                Log.DebugFormat("{0} Ignored call to SaveChanges since participating in a unit of work", _id);
            }
        }

        void InternalSaveChanges()
        {
            Log.DebugFormat("{0} saving changes. Unit of work: {1}",_id, _unitOfWork ?? (object)"null");
            _handledDocuments.ForEach(p => p.Value.CommitChangesToBackingStore());
        }

        public virtual IEnumerable<T> GetAll<T>() where T : IHasPersistentIdentity<Guid>
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            var stored = _backingStore.GetAll<T>();
            stored.Where(document => !_idMap.Contains(typeof (T), document.Id))
                .ForEach(unloadedDocument => OnInitialLoad(unloadedDocument.Id, unloadedDocument));
            return _idMap.Select(pair => pair.Value).OfType<T>();
        }

        public IEnumerable<Guid> GetAllIds<T>() where T : IHasPersistentIdentity<Guid> => _backingStore.GetAllIds<T>();

        public virtual void Dispose()
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            //Can be called before the transaction commits....
            //_idMap.Clear();
        }

        public override string ToString() => "{0}: {1}".FormatWith(_id, GetType().FullName);

        IUnitOfWork _unitOfWork;
        readonly Guid _id = Guid.NewGuid();
        Dictionary<Type, Dictionary<string, string>> _persistentValues = new Dictionary<Type, Dictionary<string, string>>();


        IUnitOfWork IUnitOfWorkParticipant.UnitOfWork => _unitOfWork;
        Guid IUnitOfWorkParticipant.Id => _id;

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