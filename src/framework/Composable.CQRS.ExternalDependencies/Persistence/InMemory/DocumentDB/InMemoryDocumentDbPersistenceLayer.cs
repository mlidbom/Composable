using System;
using System.Collections.Generic;
using Composable.Persistence.DocumentDb;

namespace Composable.Persistence.InMemory.DocumentDB
{
    //urgent: implement InMemoryDocumentDbPersistenceLayer
    class InMemoryDocumentDbPersistenceLayer : IDocumentDbPersistenceLayer
    {
        public void Add(string idString, Guid typeIdGuid, DateTime now, string serializedDocument) { throw new NotImplementedException(); }

        public bool TryGet(string idString, IReadOnlyList<Guid> acceptableTypeIds, bool useUpdateLock, out IDocumentDbPersistenceLayer.ReadRow? document) => throw new NotImplementedException();

        public void Update(IReadOnlyList<IDocumentDbPersistenceLayer.WriteRow> toUpdate) { throw new NotImplementedException(); }

        public int Remove(string idString, IReadOnlyList<Guid> acceptableTypes) => throw new NotImplementedException();

        public IEnumerable<Guid> GetAllIds(IReadOnlyList<Guid> acceptableTypes) => throw new NotImplementedException();

        public IReadOnlyList<IDocumentDbPersistenceLayer.ReadRow> GetAll(IEnumerable<Guid> ids, IReadOnlyList<Guid> acceptableTypes) => throw new NotImplementedException();

        public IReadOnlyList<IDocumentDbPersistenceLayer.ReadRow> GetAll(IReadOnlyList<Guid> acceptableTypeIds) => throw new NotImplementedException();
    }
}
