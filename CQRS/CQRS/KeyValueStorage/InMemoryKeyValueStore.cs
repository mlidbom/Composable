using System;
using System.Collections.Generic;

namespace Composable.KeyValueStorage
{
    public class InMemoryKeyValueStore : IKeyValueStore
    {
        public Dictionary<Guid, object > _store = new Dictionary<Guid, object>();

        public IKeyValueSession OpenSession()
        {
            return new InMemoryKeyValueStoreSession(this);
        }
    }
}