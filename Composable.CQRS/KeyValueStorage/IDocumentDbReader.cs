using System;
using System.Collections.Generic;
using Composable.DDD;

namespace Composable.KeyValueStorage
{
    public interface IDocumentDbReader : IDisposable
    {
        TValue Get<TValue>(object key);
        bool TryGet<TValue>(object key, out TValue document);        
        IEnumerable<T> Get<T>(IEnumerable<Guid> ids ) where T : IHasPersistentIdentity<Guid>;
    }
}