using System;
using System.Collections.Generic;
using Composable.DDD;

namespace Composable.Persistence.DocumentDb
{
    public interface IDocumentDbBulkReader : IDocumentDbReader
    {
        IEnumerable<T> GetAll<T>() where T : IHasPersistentIdentity<Guid>;
        IEnumerable<Guid> GetAllIds<T>() where T : IHasPersistentIdentity<Guid>;
    }
}