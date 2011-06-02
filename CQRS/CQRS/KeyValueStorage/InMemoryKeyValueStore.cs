using System;
using System.Collections.Generic;

namespace Composable.KeyValueStorage
{
    public class InMemoryKeyValueStore : IKeyValueStore
    {
        internal Dictionary<Type, Dictionary<Guid, object>> _store = new Dictionary<Type, Dictionary<Guid, object>>();

        public IKeyValueStoreSession OpenSession()
        {
            return new InMemoryKeyValueSession(this);
        }
    }
}