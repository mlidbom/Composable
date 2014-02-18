using System;
using System.Collections.Generic;
using Composable.DDD;

namespace Composable.KeyValueStorage
{
    public interface IDocumentDbBulkReader : IDocumentDbReader
    {
        IEnumerable<T> GetAll<T>() where T : IHasPersistentIdentity<Guid>;
    }
}