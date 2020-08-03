using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Transactions;
using Composable.Contracts;
using Composable.DDD;
using Composable.Logging;
using Composable.SystemCE.CollectionsCE.GenericCE;
using Composable.SystemCE.LinqCE;
using Composable.SystemCE.ThreadingCE;
using Composable.SystemCE.TransactionsCE;

namespace Composable.Persistence.DocumentDb
{
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    partial class DocumentDbSession : IDocumentDbSession
    {
        readonly MemoryObjectStore _idMap = new MemoryObjectStore();

        readonly IDocumentDb _backingStore;
        readonly ISingleContextUseGuard _usageGuard;

        readonly IDictionary<DocumentKey, DocumentItem> _handledDocuments = new Dictionary<DocumentKey, DocumentItem>();

        static readonly ILogger Log = Logger.For<DocumentDbSession>();

        public DocumentDbSession(IDocumentDb backingStore)
        {
            _usageGuard = new CombinationUsageGuard(new SingleThreadUseGuard(), new SingleTransactionUsageGuard());
            _backingStore = backingStore;

            _transactionParticipant = new VolatileLambdaTransactionParticipant(EnlistmentOptions.EnlistDuringPrepareRequired, onPrepare: FlushChanges);
        }

        public virtual bool TryGet<TValue>(object key, [NotNullWhen(true)]out TValue document) => TryGetInternal(key, typeof(TValue), out document, useUpdateLock: false);

        bool TryGetInternal<TValue>(object key, Type documentType, [NotNullWhen(true)]out TValue value, bool useUpdateLock)
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            _transactionParticipant.EnsureEnlistedInAnyAmbientTransaction();
            if (documentType.IsInterface)
            {
                throw new ArgumentException("You cannot query by id for an interface type. There is no guarantee of uniqueness");
            }

            if (_idMap.TryGet(key, out value) && documentType.IsInstanceOfType(value))
            {
                return true;
            }

            var documentItem = GetDocumentItem(key, documentType);
            if(!documentItem.IsDeleted && _backingStore.TryGet(key, out value, _persistentValues, useUpdateLock) && documentType.IsInstanceOfType(value))
            {
                OnInitialLoad(key, value!);
                return true;
            }

            return false;
        }

        DocumentItem GetDocumentItem(object key, Type documentType)
        {
            var documentKey = new DocumentKey(key, documentType);

            if (!_handledDocuments.TryGetValue(documentKey, out var doc))
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

        public virtual TValue GetForUpdate<TValue>(object key) =>
            GetInternal<TValue>(key, useUpdateLock: true);

        public IEnumerable<TValue> GetAll<TValue>(IEnumerable<Guid> ids) where TValue : IHasPersistentIdentity<Guid>
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
            _transactionParticipant.EnsureEnlistedInAnyAmbientTransaction();
            if (TryGet(key, out TValue value))
            {
                return value!;
            }

            throw new NoSuchDocumentException(key, typeof(TValue));
        }

        TValue GetInternal<TValue>(object key, bool useUpdateLock)
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            _transactionParticipant.EnsureEnlistedInAnyAmbientTransaction();
            if (TryGetInternal(key, typeof(TValue), out TValue value, useUpdateLock))
            {
                return value!;
            }

            throw new NoSuchDocumentException(key, typeof(TValue));
        }

        public virtual void Save<TValue>(object id, TValue value)
        {
            Contract.ArgumentNotNull(value, nameof(value));
            _usageGuard.AssertNoContextChangeOccurred(this);
            _transactionParticipant.EnsureEnlistedInAnyAmbientTransaction();

            if (TryGetInternal(id, value.GetType(), out TValue _, useUpdateLock: false))
            {
                throw new AttemptToSaveAlreadyPersistedValueException(id, value);
            }

            var documentItem = GetDocumentItem(id, value.GetType());
            documentItem.Save(value);

            _idMap.Add(id, value);
            documentItem.CommitChangesToBackingStore();
        }

        public virtual void Save<TEntity>(TEntity entity) where TEntity : IHasPersistentIdentity<Guid>
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            _transactionParticipant.EnsureEnlistedInAnyAmbientTransaction();

            if (entity.Id.Equals(Guid.Empty))
            {
                throw new DocumentIdIsEmptyGuidException();
            }
            Save(entity.Id, entity);
        }

        public virtual void Delete<TEntity>(TEntity entity) where TEntity : IHasPersistentIdentity<Guid>
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            _transactionParticipant.EnsureEnlistedInAnyAmbientTransaction();

            Delete<TEntity>(entity.Id);
        }

        public virtual void Delete<T>(object id)
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            _transactionParticipant.EnsureEnlistedInAnyAmbientTransaction();

            if(!TryGet(id, out T _))
            {
                throw new NoSuchDocumentException(id, typeof(T));
            }

            var documentItem = GetDocumentItem(id, typeof(T));
            documentItem.Delete();

            _idMap.Remove(id, typeof(T));
            documentItem.CommitChangesToBackingStore();
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

        public override string ToString() => $"{_id}: {GetType().FullName}";

        readonly Guid _id = Guid.NewGuid();
        readonly Dictionary<Type, Dictionary<string, string>> _persistentValues = new Dictionary<Type, Dictionary<string, string>>();

        readonly VolatileLambdaTransactionParticipant _transactionParticipant;
        void FlushChanges() => _handledDocuments.ForEach(p => p.Value.CommitChangesToBackingStore());
    }
}