﻿using System;
using System.Collections.Generic;
using System.Linq;
using Composable.DDD;
using Composable.Persistence.DocumentDb;
using Composable.SystemExtensions.Threading;

namespace Composable.Persistence.EventStore.Query.Models.Generators
{
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class QueryModelGeneratingDocumentDbReader : IVersioningDocumentDbReader
    {
        readonly ISingleContextUseGuard _usageGuard;
        readonly IEnumerable<IQueryModelGenerator> _documentGenerators;
        readonly InMemoryObjectStore _idMap = new InMemoryObjectStore();
        public QueryModelGeneratingDocumentDbReader(IEnumerable<IQueryModelGenerator> documentGenerators )
        {
            _usageGuard = new SingleThreadUseGuard();
            _documentGenerators = documentGenerators;
        }

        public void Dispose()
        {
        }

        public virtual TValue Get<TValue>(object key)
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            if (TryGet(key, out TValue value))
            {
                return value;
            }

            throw new NoSuchDocumentException(key, typeof(TValue));
        }

        public virtual TValue GetVersion<TValue>(object key, int version)
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            if (TryGetVersion(key, out TValue value, version))
            {
                return value;
            }

            throw new NoSuchDocumentException(key, typeof(TValue));
        }

        public virtual bool TryGet<TDocument>(object key, out TDocument document) => TryGetVersion(key, out document);

        public virtual bool TryGetVersion<TDocument>(object key, out TDocument document, int version = -1)
        {
            var requiresVersioning = version > 0;
            _usageGuard.AssertNoContextChangeOccurred(this);

            if (!HandlesDocumentType<TDocument>(requireVersioningSupport: requiresVersioning))
            {
                document = default;
                return false;
            }

            var documentType = typeof(TDocument);

            if (documentType.IsInterface)
            {
                throw new ArgumentException("You cannot query by id for an interface type. There is no guarantee of uniqueness");
            }

            if (!requiresVersioning && _idMap.TryGet(key, out document) && documentType.IsInstanceOfType(document))
            {
                return true;
            }

            document = TryGenerateModel<TDocument>(key, version);
            if (!Equals(document, default(TDocument)))
            {
                if(!requiresVersioning)
                {
                    _idMap.Add(key, document);
                }
                return true;
            }
            return false;
        }

        TDocument TryGenerateModel<TDocument>(object key, int version)
        {
            if(version < 0)
            {
                return GetGeneratorsForDocumentType<TDocument>()
                    .Select(generator => generator.TryGenerate((Guid)key))
                    .Where(foundDocument => !Equals(foundDocument, default(TDocument)))
                    .SingleOrDefault();
            }

            return VersionedGeneratorsForDocumentType<TDocument>()
                    .Select(generator => generator.TryGenerate((Guid)key, version))
                    .Where(foundDocument => !Equals(foundDocument, default(TDocument)))
                    .SingleOrDefault();
        }

        bool HandlesDocumentType<TDocument>(bool requireVersioningSupport) => requireVersioningSupport
                                                                                  ? VersionedGeneratorsForDocumentType<TDocument>().Any()
                                                                                  : GetGeneratorsForDocumentType<TDocument>().Any();

        public virtual IEnumerable<TValue> Get<TValue>(IEnumerable<Guid> ids) where TValue : IHasPersistentIdentity<Guid>
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            return ids.Select(id => Get<TValue>(id)).ToList();
        }

        IEnumerable<IVersioningQueryModelGenerator<TDocument>> VersionedGeneratorsForDocumentType<TDocument>() => _documentGenerators.OfType<IVersioningQueryModelGenerator<TDocument>>().ToList();

        IEnumerable<IQueryModelGenerator<TDocument>> GetGeneratorsForDocumentType<TDocument>() => _documentGenerators.OfType<IQueryModelGenerator<TDocument>>().ToList();
    }
}